package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Test

class ProductionRulesTest {
    @Test
    fun `label count rounds up and whole quantity uses one label`() {
        assertEquals(3, productionLabelCount(21.0, 10.0))
        assertEquals(1, productionLabelCount(21.0, 1.0))
        assertEquals(1, productionLabelCount(21.0, 0.0))
    }

    @Test
    fun `return ceiling is net consumption of the lot`() {
        assertEquals(7.5, returnableQty(10.0, 2.5), 0.0001)
        assertEquals(0.0, returnableQty(2.0, 3.0), 0.0001)
    }

    @Test
    fun `near expiry warning follows FEFO allocation`() {
        val later = StockLine(availableQty = 10.0, expiryDate = "2026-12-01", remainingDays = 36)
        val near = StockLine(availableQty = 2.0, expiryDate = "2026-11-01", remainingDays = 6)
        assertEquals(true, nearExpiryFefo(listOf(later, near), 1.0))
        assertEquals(false, nearExpiryFefo(listOf(later), 1.0))
    }

    @Test
    fun `reversed work no longer contributes to return ceiling`() {
        val rows = listOf(ProductionWork(no = 4, qty = 5.0), ProductionWork(no = 5, reversesNo = 4, qty = -5.0))
        assertEquals(emptyList<ProductionWork>(), activeProductionWorks(rows))
    }

    @Test
    fun `production menu requires view grant and fails closed`() {
        assertEquals(emptySet<String>(), productionMenuKeys(null))
        val permissions = MyPermissions(functions = listOf(
            PermissionGrant("ht_prd_input", listOf("view")),
            PermissionGrant("ht_prd_output", listOf("edit")),
            PermissionGrant("prd_import", listOf("view")),
        ))
        assertEquals(setOf("ht_prd_input"), productionMenuKeys(permissions))
        assertEquals(4, productionMenuKeys(MyPermissions(isSuperAdmin = true)).size)
    }

    @Test
    fun `label plan splits into full packages and one remainder`() {
        assertEquals(LabelPlan(count = 3, full = 2, remainder = 1.0, std = 10.0), productionLabelPlan(21.0, 10.0))
        assertEquals(LabelPlan(count = 2, full = 2, remainder = 0.0, std = 10.0), productionLabelPlan(20.0, 10.0))
        assertEquals(LabelPlan(count = 1, full = 0, remainder = 21.0, std = 1.0), productionLabelPlan(21.0, 1.0))
        assertEquals(0, productionLabelPlan(0.0, 10.0).count)
    }

    @Test
    fun `material preview follows the lot rule and FEFO`() {
        val a = StockLine(serialNo = "A", lotNo = "L1", availableQty = 4.0, expiryDate = "2027-01-01", remainingDays = 60)
        val b = StockLine(serialNo = "B", lotNo = "L1", availableQty = 3.0, expiryDate = "2026-11-10", remainingDays = 10)
        val held = StockLine(serialNo = "H", lotNo = "L1", availableQty = 9.0, isOnHold = true)
        val old = StockLine(serialNo = "O", lotNo = "L2", availableQty = 5.0, expiryDate = "2026-01-01", remainingDays = -30)
        val stocks = listOf(a, b, held, old)
        assertEquals("LOT-001", materialPreview(stocks, lotManaged = true, lot = null).code)
        assertEquals("SCN-006", materialPreview(stocks, lotManaged = true, lot = "L9").code)
        assertEquals("INV-001", materialPreview(stocks, lotManaged = true, lot = "L2").code)
        val ok = materialPreview(stocks, lotManaged = true, lot = "L1", qty = 2.0)
        assertEquals(null, ok.code)
        assertEquals(7.0, ok.available, 0.0001)
        assertEquals(true, ok.nearExpiry)
        assertEquals(false, materialPreview(listOf(a), lotManaged = true, lot = "L1", qty = 2.0).nearExpiry)
        assertEquals("INV-009", materialPreview(stocks, lotManaged = true, lot = "L1", qty = 8.0).code)
        assertEquals("INV-009", materialPreview(emptyList(), lotManaged = false, lot = null).code)
        assertEquals("HLD-001", materialPreview(listOf(held), lotManaged = true, lot = "L1").code)
    }

    @Test
    fun `process scan must be an active HT process of the order`() {
        val lines = listOf(
            ProductionLine(lineNo = 1, processCode = "MIX", htScanRequired = true),
            ProductionLine(lineNo = 2, processCode = "PACK", htScanRequired = false),
            ProductionLine(lineNo = 3, processCode = "OLD", htScanRequired = true, processActive = false),
        )
        assertEquals(null, processScanError(lines, "MIX"))
        assertEquals("SCN-010", processScanError(lines, "PACK"))
        assertEquals("SCN-010", processScanError(lines, "NONE"))
        assertEquals("SYS-404", processScanError(lines, "OLD"))
    }

    @Test
    fun `material scan must match an HT line of the scanned process`() {
        val lines = listOf(
            ProductionLine(lineNo = 1, processCode = "MIX", itemCode = "RM1", htScanRequired = true),
            ProductionLine(lineNo = 2, processCode = "PACK", itemCode = "RM2", htScanRequired = true),
        )
        assertEquals(null, materialScanError(lines, "MIX", "RM1"))
        assertEquals("SCN-005", materialScanError(lines, "MIX", "RM2"))
    }

    @Test
    fun `order list per screen hides cancelled and applies the status filter`() {
        val orders = listOf(
            ProductionOrder(productionOrderNo = "MO-1", statusCode = 0),
            ProductionOrder(productionOrderNo = "MO-2", statusCode = 2),
            ProductionOrder(productionOrderNo = "MO-3", statusCode = 3),
            ProductionOrder(productionOrderNo = "MO-4", statusCode = 1),
        )
        val lines = listOf(
            ProductionLine(productionOrderNo = "MO-1", htScanRequired = true),
            ProductionLine(productionOrderNo = "MO-4", htScanRequired = false, consumedQty = 2.0),
        )
        assertEquals(listOf("MO-4", "MO-1"), productionOrdersFor("receipt", orders, lines, "active", "").map { it.productionOrderNo })
        assertEquals(listOf("MO-2"), productionOrdersFor("output", orders, lines, "completed", "").map { it.productionOrderNo })
        assertEquals(listOf("MO-1"), productionOrdersFor("input", orders, lines, "all", "").map { it.productionOrderNo })
        assertEquals(listOf("MO-4"), productionOrdersFor("return", orders, lines, "all", "").map { it.productionOrderNo })
        assertEquals(listOf("MO-4"), productionOrdersFor("receipt", orders, lines, "all", "mo-4").map { it.productionOrderNo })
    }
}
