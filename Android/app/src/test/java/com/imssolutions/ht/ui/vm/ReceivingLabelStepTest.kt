package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Test

class ReceivingLabelStepTest {

    @Test
    fun `a negative or blank quantity is refused`() {
        assertEquals("QTY-001", (labelConfirmStep(null, false, "PRN") as LabelConfirmStep.Refuse).code)
        assertEquals("QTY-001", (labelConfirmStep(-1.0, false, "PRN") as LabelConfirmStep.Refuse).code)
    }

    @Test
    fun `zero records nothing arrived without printing`() {
        assertEquals(LabelConfirmStep.Go(print = false), labelConfirmStep(0.0, false, null))
    }

    @Test
    fun `a label already printed for this package is not printed again`() {
        assertEquals(LabelConfirmStep.Go(print = false), labelConfirmStep(5.0, true, "PRN"))
    }

    @Test
    fun `no printer saved is PRN-001 rather than leaving the screen`() {
        assertEquals("PRN-001", (labelConfirmStep(5.0, false, " ") as LabelConfirmStep.Refuse).code)
    }

    @Test
    fun `otherwise print then confirm`() {
        assertEquals(LabelConfirmStep.Go(print = true), labelConfirmStep(5.0, false, "PRN"))
    }
}
