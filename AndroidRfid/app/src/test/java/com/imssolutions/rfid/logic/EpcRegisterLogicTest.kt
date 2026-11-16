package com.imssolutions.rfid.logic

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class EpcRegisterLogicTest {

    private val a1 = "E2801000000000000000A001"
    private val a2 = "E2801FFFFFFFFFFFFFFFB002"
    private val b1 = "E2802000000000000000A001"

    @Test
    fun `only hex EPCs of 5 to 24 characters can be registered`() {
        assertEquals(a1, registrableEpc(" e2801000000000000000a001 "))
        assertNull(registrableEpc("ABCD"))
        assertEquals("ABCDE", registrableEpc("abcde"))
        assertNull(registrableEpc("E2801170000Z"))
        assertNull(registrableEpc(a1 + "0"))
        assertNull(registrableEpc(null))
    }

    @Test
    fun `EPCs with the same first five characters form one group`() {
        val groups = groupEpcs(listOf(a2, b1, a1, a1.lowercase()), emptyMap())

        assertEquals(listOf("E2801", "E2802"), groups.map { it.prefix })
        assertEquals(listOf(a1, a2), groups[0].newEpcs)
        assertEquals(1, groups[1].total)
    }

    @Test
    fun `registered EPCs stay in their group but are not new`() {
        val g = groupEpcs(listOf(a1, a2), mapOf(a1 to KnownPart("PN-100", "Head"))).single()

        assertEquals(listOf(a2), g.newEpcs)
        assertEquals("PN-100", g.registered.getValue(a1).code)
    }

    @Test
    fun `a plan needs a part no and name for every group with new tags`() {
        val groups = groupEpcs(listOf(a1, b1), emptyMap())

        assertEquals(RegisterPlan.Missing("E2801"), registerPlan(groups, emptyMap()))
        val partial = mapOf("E2801" to PartInput("PN-100", "Head"))
        assertEquals(RegisterPlan.Missing("E2802"), registerPlan(groups, partial))
    }

    @Test
    fun `a ready plan trims the inputs and counts tags and parts`() {
        val groups = groupEpcs(listOf(a1, a2, b1), emptyMap())
        val plan = registerPlan(groups, mapOf(
            "E2801" to PartInput(" PN-100 ", " Head "),
            "E2802" to PartInput("PN-100", "Head"),
        ))

        assertTrue(plan is RegisterPlan.Ready)
        plan as RegisterPlan.Ready
        assertEquals(RegisterLine("PN-100", "Head", listOf(a1, a2)), plan.lines[0])
        assertEquals(3, plan.tagCount)
        assertEquals(1, plan.partCount)
    }

    @Test
    fun `groups whose tags are all registered need no input`() {
        val groups = groupEpcs(listOf(a1), mapOf(a1 to KnownPart("PN-100", "Head")))
        assertEquals(RegisterPlan.Nothing, registerPlan(groups, emptyMap()))
    }

    @Test
    fun `an over-long part no is refused before sending`() {
        val groups = groupEpcs(listOf(a1), emptyMap())
        val plan = registerPlan(groups, mapOf("E2801" to PartInput("P".repeat(51), "Head")))
        assertEquals(RegisterPlan.TooLong("E2801"), plan)
    }

    @Test
    fun `prefill fills a known part but never overwrites what the user typed`() {
        val known = mapOf(
            "P1" to KnownPart("PN-100", "Head"),
            "P2" to KnownPart("PN-200", "Platen"),
        )
        val next = prefillInputs(mapOf("P2" to PartInput("MINE", "")), known)

        assertEquals(PartInput("PN-100", "Head"), next["P1"])
        assertEquals(PartInput("MINE", ""), next["P2"])
    }

    @Test
    fun `a read of an excluded group or one already read is not accepted`() {
        val reads = setOf(a1)
        assertNull(acceptRegisterRead(reads, emptySet(), a1.lowercase()))
        assertNull(acceptRegisterRead(reads, setOf("E2802"), b1))
        assertNull(acceptRegisterRead(reads, emptySet(), "XYZ"))
        assertEquals(a2, acceptRegisterRead(reads, setOf("E2802"), a2))
    }

    @Test
    fun `removing a group drops only the tags of that prefix`() {
        assertEquals(setOf(b1), withoutGroup(setOf(a1, a2, b1), "E2801"))
        assertEquals(setOf(a1), withoutGroup(setOf(a1), "E2802"))
    }
}
