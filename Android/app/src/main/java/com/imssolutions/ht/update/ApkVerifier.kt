package com.imssolutions.ht.update

import android.content.Context
import android.content.pm.PackageInfo
import android.content.pm.PackageManager
import android.content.pm.Signature
import android.os.Build
import java.io.File
import java.security.MessageDigest

object ApkVerifier {

    enum class Failure { HASH, PACKAGE, VERSION, SIGNATURE }

    fun sha256(file: File): String {
        val md = MessageDigest.getInstance("SHA-256")
        file.inputStream().use { input ->
            val buf = ByteArray(64 * 1024)
            while (true) {
                val n = input.read(buf)
                if (n < 0) break
                md.update(buf, 0, n)
            }
        }
        return md.digest().joinToString("") { "%02x".format(it) }
    }

    @Suppress("DEPRECATION")
    fun verify(context: Context, file: File, entry: VersionEntry): Failure? {
        if (entry.sha256.isBlank() || !sha256(file).equals(entry.sha256.trim(), ignoreCase = true)) return Failure.HASH
        val pm = context.packageManager
        val flags = if (Build.VERSION.SDK_INT >= 28) PackageManager.GET_SIGNING_CERTIFICATES else PackageManager.GET_SIGNATURES
        val archive = pm.getPackageArchiveInfo(file.absolutePath, flags) ?: return Failure.PACKAGE
        if (archive.packageName != context.packageName || archive.packageName != entry.packageName) return Failure.PACKAGE
        if (codeOf(archive) != entry.versionCode) return Failure.VERSION
        val installed = pm.getPackageInfo(context.packageName, flags)
        return if (sameSigner(installed, archive)) null else Failure.SIGNATURE
    }

    @Suppress("DEPRECATION")
    private fun codeOf(info: PackageInfo): Long =
        if (Build.VERSION.SDK_INT >= 28) info.longVersionCode else info.versionCode.toLong()

    @Suppress("DEPRECATION")
    private fun sameSigner(installed: PackageInfo, archive: PackageInfo): Boolean {
        if (Build.VERSION.SDK_INT >= 28) {
            val mine = installed.signingInfo?.apkContentsSigners?.toList().orEmpty()
            val si = archive.signingInfo ?: return false
            val theirs = (if (si.hasMultipleSigners()) si.apkContentsSigners else si.signingCertificateHistory)?.toList().orEmpty()
            if (mine.isEmpty() || theirs.isEmpty()) return false
            return if (si.hasMultipleSigners()) keys(mine) == keys(theirs) else keys(theirs).containsAll(keys(mine))
        }
        val mine = installed.signatures?.toList().orEmpty()
        val theirs = archive.signatures?.toList().orEmpty()
        return mine.isNotEmpty() && keys(mine) == keys(theirs)
    }

    private fun keys(list: List<Signature>): Set<String> = list.map { it.toCharsString() }.toSet()
}
