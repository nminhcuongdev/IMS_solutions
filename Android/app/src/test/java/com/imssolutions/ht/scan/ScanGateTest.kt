package com.imssolutions.ht.scan

import org.junit.After
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class ScanGateTest {

    @After
    fun reset() {
        repeat(5) { ScanGate.unblock() }
    }

    @Test
    fun `mac dinh khong chan`() {
        assertFalse(ScanGate.blocked)
    }

    @Test
    fun `hai lop chong nhau go mot lop van con chan`() {
        ScanGate.block()
        ScanGate.block()
        ScanGate.unblock()
        assertTrue(ScanGate.blocked)
        ScanGate.unblock()
        assertFalse(ScanGate.blocked)
    }

    @Test
    fun `go thua khong lam khoa am roi ket mo`() {
        ScanGate.unblock()
        ScanGate.block()
        assertTrue(ScanGate.blocked)
    }
}
