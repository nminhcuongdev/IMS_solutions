namespace Ims.Api.Contracts;

public static class ErrorCatalog
{
    public static ApiErrorBody Val001() =>
        new("VAL-001", 400, "Error", "コードは必須です。");
    public static ApiErrorBody Val002() =>
        new("VAL-002", 400, "Error", "名称を入力してください。");
    public static ApiErrorBody Val004() =>
        new("VAL-004", 400, "Error", "有効なIPアドレスを入力してください（0〜255の数値とピリオドのみ使用可）。");
    public static ApiErrorBody Val005() =>
        new("VAL-005", 400, "Error", "以下の文字は使用できません: / ? & = # %");

    public static ApiErrorBody Val005TooLong(string code, int max) =>
        new("VAL-005", 400, "Error",
            $"コードは{max}文字以内で入力してください（現在 {code.Length} 文字: {code}）。");

    public static ApiErrorBody Val005FieldTooLong() =>
        new("VAL-005", 400, "Error", "入力値が項目の最大文字数を超えています。");

    public static ApiErrorBody Val006() =>
        new("VAL-006", 400, "Error", "終了日は開始日以降にしてください。");

    public static ApiErrorBody Sel002() =>
        new("SEL-002", 400, "Error", "削除する行を1件以上選択してください。");

    public static ApiErrorBody Sel006() =>
        new("SEL-006", 400, "Error", "ステータスを選択してください。");

    public static ApiErrorBody Qty001() =>
        new("QTY-001", 400, "Error", "有効な数量（0以上）を入力してください。");

    public static ApiErrorBody Qty002() =>
        new("QTY-002", 400, "Error", "スキャンした数量が予定数量を超えています。数量をご確認ください。");

    public static ApiErrorBody Qty003() => new("QTY-003", 400, "Error", "0より大きい数量を入力してください。");
    public static ApiErrorBody Qty004() => new("QTY-004", 409, "Error", "数量が、正味の投入数量（投入 − 返却）を超えています。");
    public static ApiErrorBody Val008() => new("VAL-008", 400, "Error", "単位が品目マスタの単位と一致しません。");
    public static ApiErrorBody Val009() => new("VAL-009", 400, "Error", "この品目は品目グループの区分のため、ここでは使用できません。");
    public static ApiErrorBody Val010() => new("VAL-010", 400, "Error", "製造する製品はロット管理対象の品目にしてください。");

    public static ApiErrorBody Rsn001() =>
        new("RSN-001", 400, "Error", "理由を選択してください。");

    public static ApiErrorBody Dup001() =>
        new("DUP-001", 409, "Error", "このコードは既に使用されています。別のコードを入力してください。");

    public static ApiErrorBody Del001() =>
        new("DEL-001", 409, "Error", "このデータは他のデータから参照されているため削除できません。");

    public static ApiErrorBody Dup002() =>
        new("DUP-002", 409, "Error", "このラベルは既に登録されています。");

    public static ApiErrorBody Dup003() => new("DUP-003", 409, "Error", "このロット番号は同じ品目ですでに使用されています。");
    public static ApiErrorBody Sts004() => new("STS-004", 409, "Error", "完了済みまたは取消済みの製造指示には実績を登録できません。");
    public static ApiErrorBody Sts002() => new("STS-002", 409, "Error", "選択した行はすでに完了しています。");
    public static ApiErrorBody Sts003() => new("STS-003", 409, "Error", "選択した行の状態が混在しています。");
    public static ApiErrorBody Sts005() => new("STS-005", 409, "Error", "実績のある製造指示は取消できません。先に実績を取消してください。");
    public static ApiErrorBody Scn010() => new("SCN-010", 409, "Error", "スキャンした工程は、この製造指示のHT投入対象ではありません。");
    public static ApiErrorBody Loc004() => new("LOC-004", 409, "Error", "このロケーションは、製造指示の工場に属していません。");
    public static ApiErrorBody Inv008() => new("INV-008", 409, "Error", "この実績はすでに取消済みです。");
    public static ApiErrorBody Inv009() => new("INV-009", 409, "Error", "入力した数量に対して在庫が不足しています。");
    public static ApiErrorBody Auth006() => new("AUTH-006", 403, "Error", "この操作を行う権限がありません。");
    public static ApiErrorBody Imp004() => new("IMP-004", 400, "Error", "同じ製造指示番号の行で、製造指示の項目が一致しません。ファイルをご確認ください。");
    public static ApiErrorBody Imp005() => new("IMP-005", 400, "Error", "同じ製造指示内で行番号（Line No）が重複しています。");


    public static ApiErrorBody Lot001() =>
        new("LOT-001", 400, "Error", "ロット番号を入力してください。");

    public static ApiErrorBody Lot002() =>
        new("LOT-002", 404, "Error", "指定されたロット番号の在庫が存在しません。");

    public static ApiErrorBody Loc001() =>
        new("LOC-001", 400, "Error", "ロケーションを入力してください。");

    public static ApiErrorBody Hld001() =>
        new("HLD-001", 409, "Error", "この在庫は出荷停止中です。");

    public static ApiErrorBody Inv005() =>
        new("INV-005", 409, "Error", "この在庫は現在利用できません。");

    public static ApiErrorBody Inv006() =>
        new("INV-006", 409, "Error", "この在庫は出荷済みです。");

    public static ApiErrorBody Inv001() => new("INV-001", 409, "Error", "使用期限が切れています。");

    public static ApiErrorBody Scn006() =>
        new("SCN-006", 409, "Error", "指定されたLotと一致しません。");


    public static ApiErrorBody Stk001() =>
        new("STK-001", 409, "Error", "棚卸が開始されていません。PCで開始してください。");

    public static ApiErrorBody Stk004() =>
        new("STK-004", 409, "Error", "棚卸は既に完了しています。実績を送信できません。");

    public static ApiErrorBody Loc003() =>
        new("LOC-003", 404, "Error", "指定されたロケーションに在庫が存在しません。");

    public static ApiErrorBody Imp001() =>
        new("IMP-001", 400, "Error", "インポートできるのは.xlsxまたは.csvファイルのみです。");

    public static ApiErrorBody Imp002() =>
        new("IMP-002", 400, "Error", "CSVのヘッダーがこの画面の想定列と一致しません。ファイルをご確認の上、再度お試しください。");

    public static ApiErrorBody Sys404(string what) => new("SYS-404", 404, "Error", $"{what}が存在しない、または無効です。");

    public static ApiErrorBody Prn001() =>
        new("PRN-001", 500, "Error",
            "Label Engineからのプレビュー取得に失敗しました。アプリケーションが起動していること、プリンタドライバー名が正しいことを確認してください。");

    public static ApiErrorBody Prn002() =>
        new("PRN-002", 500, "Error",
            "Label Engine経由での印刷に失敗しました。アプリケーションが起動していること、プリンタドライバー名が正しいことを確認してください。");

    public static ApiErrorBody NotFound(string what) =>
        new("SYS-404", 404, "Error", $"{what} が見つかりません。");

    public static ApiErrorBody Db001() =>
        new("DB-001", 500, "Critical", "DBに接続できません。");

    public static ApiErrorBody Sys004() =>
        new("SYS-004", 500, "Error", "APIからエラーレスポンスを受信しました。");

    public static ApiErrorBody Auth003() =>
        new("AUTH-003", 401, "Error", "指定されたUser IDが存在しません。");
    public static ApiErrorBody Auth004() =>
        new("AUTH-004", 401, "Error", "パスワードが一致しません。");
    public static ApiErrorBody Auth005() =>
        new("AUTH-005", 401, "Error", "トークンが無効または期限切れです。");
    public static ApiErrorBody Auth001() =>
        new("AUTH-001", 400, "Error", "User IDとPasswordを入力してください。");
}
