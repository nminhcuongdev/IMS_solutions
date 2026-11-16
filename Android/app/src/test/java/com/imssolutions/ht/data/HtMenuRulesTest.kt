package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class HtMenuRulesTest {
    @Test
    fun `no permissions greys out every feature`() {
        assertEquals(emptySet<String>(), htMenuKeys(null))
        assertFalse(htMenuEnabled("ht_receiving", htMenuKeys(null)))
    }

    @Test
    fun `only Open rows of HT features count and builtin gets all`() {
        val p = MyPermissions(functions = listOf(
            PermissionGrant("ht_receiving", listOf("Open")),
            PermissionGrant("ht_prd_input", listOf("Open")),
            PermissionGrant("rcv_import", listOf("Open")),
            PermissionGrant("ht_picking", listOf("New")),
        ))
        val keys = htMenuKeys(p)
        assertEquals(setOf("ht_receiving", "ht_prd_input"), keys)
        assertTrue(htMenuEnabled("ht_production", keys))
        assertFalse(htMenuEnabled("ht_picking", keys))
        assertEquals(12, htMenuKeys(MyPermissions(isBuiltin = true)).size)
    }

    @Test
    fun `worker with receiving and material input sees three categories and common greyed`() {
        val access = htMenuAccess(MyPermissions(functions = listOf(
            PermissionGrant("ht_receiving", listOf("Open")),
            PermissionGrant("ht_prd_input", listOf("Open")),
        )))
        val (wh, prd, cm) = HT_CATEGORIES
        assertTrue(HT_CATEGORIES.all { htCategoryVisible(it, access) })
        assertTrue(htCategoryEnabled(wh, access))
        assertTrue(htCategoryEnabled(prd, access))
        assertFalse(htCategoryEnabled(cm, access))
        assertEquals(listOf("ht_receiving"), htCategoryFeatures(wh, access).filter { it in access.allowed })
    }

    @Test
    fun `a function hidden by Super Admin is hidden, not greyed`() {
        val access = htMenuAccess(MyPermissions(isBuiltin = true, visibleFunctions = listOf("ht_receiving", "ht_disposal")))
        assertEquals(listOf("ht_receiving"), htCategoryFeatures(HT_CATEGORIES[0], access))
        assertFalse(htCategoryVisible(HT_CATEGORIES[1], access))
        assertEquals(setOf("ht_receiving", "ht_disposal"), access.allowed)
    }
}
