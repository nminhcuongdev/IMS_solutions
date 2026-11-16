package com.imssolutions.ht

import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

class SourceTextTest {
    private val vietnamese = Regex("[ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹĂÂĐÊÔƠƯẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼẾỀỂỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỲỴỶỸ]")
    private val literal = Regex("\"(?:[^\"\\\\]|\\\\.)*\"")
    private val nativeLanguageNames = setOf("HtLang.kt")

    @Test
    fun `no Kotlin string literal under src main contains Vietnamese letters`() {
        val root = listOf(File("src/main/java"), File("app/src/main/java")).first { it.isDirectory }
        val offenders = root.walkTopDown()
            .filter { it.isFile && it.extension == "kt" && it.name !in nativeLanguageNames }
            .flatMap { file ->
                file.readLines().withIndex()
                    .filter { (_, line) -> literal.findAll(line).any { vietnamese.containsMatchIn(it.value) } }
                    .map { (i, line) -> "${file.name}:${i + 1}: ${line.trim()}" }
            }
            .toList()
        assertTrue("Vietnamese literals: " + offenders.joinToString("\n"), offenders.isEmpty())
    }
}
