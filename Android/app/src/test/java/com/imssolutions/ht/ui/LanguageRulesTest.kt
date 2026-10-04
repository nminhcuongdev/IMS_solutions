package com.imssolutions.ht.ui

import org.junit.Assert.assertEquals
import org.junit.Test
import com.imssolutions.ht.data.resolveLanguage

class LanguageRulesTest {


    @Test
    fun `khong co lua chon nao thi ve tieng Viet`() {
        assertEquals("vi", resolveLanguage(override = null, userCode = "", userDefault = null))
    }


    @Test
    fun `chua bam co thi theo ngon ngu cua tai khoan`() {
        assertEquals("ja", resolveLanguage(null, userCode = "U001", userDefault = "ja"))
    }

    @Test
    fun `ma ngon ngu la thi bo qua, khong lam sap app`() {
        assertEquals("vi", resolveLanguage(null, userCode = "U001", userDefault = "zz"))
        assertEquals("vi", resolveLanguage(null, userCode = "U001", userDefault = "  "))
    }


    @Test
    fun `lua chon cua chinh nguoi dang dang nhap thang ngon ngu tai khoan`() {
        assertEquals("en", resolveLanguage("en" to "U001", userCode = "U001", userDefault = "ja"))
    }

    @Test
    fun `lua chon cua NGUOI KHAC khong de len ngon ngu tai khoan`() {
        assertEquals("ja", resolveLanguage("en" to "U001", userCode = "U002", userDefault = "ja"))
    }

    @Test
    fun `lua chon cua nguoi khac ma tai khoan moi khong khai gi thi ve tieng Viet`() {
        assertEquals("vi", resolveLanguage("en" to "U001", userCode = "U002", userDefault = null))
    }

    @Test
    fun `chua dang nhap thi van ton trong lua chon tay`() {
        assertEquals("ja", resolveLanguage("ja" to "U001", userCode = "", userDefault = null))
    }

    @Test
    fun `co bam o man Login khong de len ngon ngu tai khoan`() {
        assertEquals("ja", resolveLanguage("en" to "", userCode = "U005", userDefault = "ja"))
    }

    @Test
    fun `co khong ro chu van duoc dung khi tai khoan khong khai gi`() {
        assertEquals("en", resolveLanguage("en" to "", userCode = "U002", userDefault = null))
    }

    @Test
    fun `bam co SAU khi dang nhap thi thang ngon ngu tai khoan`() {
        assertEquals("vi", resolveLanguage("vi" to "U005", userCode = "U005", userDefault = "ja"))
    }


    @Test
    fun `thieu khoa thi tra chu du phong chu khong tra rong`() {
        val s = HtStrings(labels = mapOf("htNext" to "次へ"))
        assertEquals("次へ", s.t("htNext", "Next"))
        assertEquals("Save", s.t("btnSave", "Save"))
    }

    @Test
    fun `nhan rong cung roi ve chu du phong`() {
        val s = HtStrings(labels = mapOf("btnSave" to "   "))
        assertEquals("Save", s.t("btnSave", "Save"))
    }

    @Test
    fun `tham so duoc thay theo so thu tu`() {
        val s = HtStrings(labels = mapOf("htRowCount" to "{0} 件"))
        assertEquals("7 件", s.t("htRowCount", "{0} dòng", 7))
    }

    @Test
    fun `chu du phong cung duoc thay tham so`() {
        assertEquals("7 dòng", HtStrings().t("htRowCount", "{0} dòng", 7))
    }


    @Test
    fun `ma ngon ngu la khong lam sap thanh co`() {
        assertEquals(HtLang.JA, HtLang.of("ja"))
        assertEquals(HtLang.EN, HtLang.of("EN"))
        assertEquals(HtLang.VI, HtLang.of("zz"))
        assertEquals(HtLang.VI, HtLang.of(null))
    }
}
