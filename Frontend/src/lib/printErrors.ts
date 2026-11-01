import { ApiCallError } from "@/api/types";
import { errorText } from "@/lib/errorText";

const PRINT_ERRORS: Record<string | number, [code: string, fallbackVi: string]> = {
  "-6": ["PRT-N6", "Không thể lưu file layout. Vui lòng kiểm tra quyền của thư mục lưu."],
  "-5": ["PRT-N5", "Tải file layout thất bại. Vui lòng kiểm tra URL và thiết lập chứng chỉ."],
  "-3": ["PRT-N3", "Định dạng URL đã chỉ định không đúng."],
  "-2": ["PRT-N2", "Không thể sử dụng URL ngoài HTTP hoặc HTTPS."],
  "-1": ["PRT-N1", "Tên file layout không hợp lệ."],
  1: ["PRT-1", "Không thể đọc file layout. Vui lòng kiểm tra file có tồn tại hay không."],
  3: ["PRT-3", "Đã kết nối với máy in."],
  4: ["PRT-4", "Không thể kết nối với máy in. Vui lòng kiểm tra nguồn điện, địa chỉ IP, kết nối USB."],
  5: ["PRT-5", "Chưa bắt đầu kết nối máy in."],
  6: ["PRT-6", "Xử lý kết thúc kết nối máy in thất bại."],
  7: ["PRT-7", "Gửi dữ liệu đến máy in thất bại."],
  8: ["PRT-8", "Gửi dữ liệu đến máy in đã hết thời gian chờ."],
  9: ["PRT-9", "Không thể nhận phản hồi từ máy in. Vui lòng kiểm tra thiết lập truyền thông."],
  10: ["PRT-10", "Không có phản hồi từ máy in."],
  11: ["PRT-11", "Máy in đang gặp lỗi. Vui lòng kiểm tra trạng thái máy in."],
  12: ["PRT-12", "Tổ hợp thiết lập truyền thông và giao thức không hợp lệ."],
  21: ["PRT-21", "Không tìm thấy driver máy in đã chỉ định."],
  22: ["PRT-22", "Xóa tác vụ in thất bại."],
  51: ["PRT-51", "Không thể đọc thông tin layout."],
  52: ["PRT-52", "Không hỗ trợ dòng máy in được chỉ định trong layout."],
  53: ["PRT-53", "Máy in này không hỗ trợ chức năng cắt."],
  55: ["PRT-55", "Đây là chức năng không thể sử dụng với phương thức xuất đã chỉ định."],
  57: ["PRT-57", "Định dạng hoặc nội dung dữ liệu in không hợp lệ."],
  61: ["PRT-61", "Không thể sử dụng vì phiên bản file layout mới hơn hệ thống."],
  62: ["PRT-62", "Không thể sử dụng vì phiên bản Global File mới hơn."],
  63: ["PRT-63", "Không thể sử dụng vì phiên bản file nhãn Header mới hơn."],
  64: ["PRT-64", "Không thể sử dụng vì phiên bản file nhãn Tail mới hơn."],
  65: ["PRT-65", "Không thể sử dụng vì phiên bản file Global Table mới hơn."],
  66: ["PRT-66", "Không thể sử dụng vì phiên bản Global Check Table mới hơn."],
  80: ["PRT-80", "Không tìm thấy chứng chỉ client đã chỉ định."],
  81: ["PRT-81", "Có nhiều chứng chỉ client khớp với điều kiện."],
  100: ["PRT-100", "Không thể đọc file thông tin Global."],
  101: ["PRT-101", "Không thể đọc file nhãn Header."],
  102: ["PRT-102", "Không thể đọc file nhãn Tail."],
  103: ["PRT-103", "Thiết lập layout không khớp với nhãn Header／Tail."],
  130: ["PRT-130", "Đã xảy ra bất thường trong quá trình vẽ."],
  152: ["PRT-152", "Loại máy in của nhãn Header và layout chính không khớp nhau."],
  153: ["PRT-153", "Loại máy in của nhãn Tail và layout chính không khớp nhau."],
  201: ["PRT-201", "Đã xảy ra lỗi hệ thống nội bộ. Vui lòng liên hệ quản trị viên."],
  202: ["PRT-202", "Đã xảy ra lỗi hệ thống nội bộ. Vui lòng liên hệ quản trị viên."],
  403: ["PRT-403", "Không tồn tại dữ liệu master tương ứng với giá trị đã nhập."],
  404: ["PRT-404", "Không tồn tại dữ liệu đối tượng trong bảng ODBC."],
  405: ["PRT-405", "Tính toán số tiền thất bại."],
  406: ["PRT-406", "Dữ liệu đối tượng tính toán không hợp lệ."],
  407: ["PRT-407", "Tính toán Check Digit thất bại."],
  410: ["PRT-410", "Kết quả tính toán Check Digit không hợp lệ."],
  413: ["PRT-413", "Chưa thiết lập thuế suất tiêu dùng."],
  414: ["PRT-414", "Không tồn tại dữ liệu chuyển đổi trong bảng Excel."],
  415: ["PRT-415", "Định dạng mã nhận dạng ứng dụng (AI) không hợp lệ."],
  430: ["PRT-430", "Xử lý phán đoán điều kiện thất bại."],
  431: ["PRT-431", "Không tồn tại biến để lưu kết quả phán đoán điều kiện."],
  450: ["PRT-450", "Không tìm thấy bảng được tham chiếu trong thiết lập in có điều kiện."],
  470: ["PRT-470", "Cách viết thẻ định dạng không hợp lệ."],
  471: ["PRT-471", "Không thể sử dụng thẻ định dạng với biến này."],
  472: ["PRT-472", "Không thể dùng chung thẻ định dạng với chức năng tự động thêm Check Digit."],
  473: ["PRT-473", "Không thể chỉ định độ rộng font khi sử dụng thẻ định dạng."],
  500: ["PRT-500", "Không có dữ liệu đối tượng để xem trước."],
  503: ["PRT-503", "Tạo hình ảnh xem trước thất bại."],
  504: ["PRT-504", "Không thể tạo thư mục lưu ảnh xem trước."],
  505: ["PRT-505", "Không thể tạo hình ảnh xem trước."],
  506: ["PRT-506", "Vui lòng kiểm tra dung lượng ổ đĩa hoặc quyền truy cập."],
  600: ["PRT-600", "Font đã chỉ định chưa được cài đặt."],
  601: ["PRT-601", "Không thể lấy thông tin định nghĩa mã vạch."],
  602: ["PRT-602", "Dữ liệu mã vạch không hợp lệ."],
  610: ["PRT-610", "Đối tượng được đặt ngoài phạm vi in của nhãn."],
  611: ["PRT-611", "Giá trị hiệu chỉnh in khiến vị trí in nằm ngoài phạm vi."],
  700: ["PRT-700", "Không thể sử dụng thiết bị Bluetooth."],
  701: ["PRT-701", "Không tìm thấy thiết bị Bluetooth đã chỉ định."],
  702: ["PRT-702", "Mã PIN không chính xác."],
  703: ["PRT-703", "Xác thực Bluetooth thất bại."],
  800: ["PRT-800", "Không thể sử dụng biến số thứ tự khi xuất qua LAN・USB・Bluetooth."],
  801: ["PRT-801", "Không thể chỉ định số lượng phát hành vượt quá 1 sheet khi xuất trực tiếp."],
  802: ["PRT-802", "Chưa thiết lập số lượng phát hành."],
  803: ["PRT-803", "Không thể sử dụng Form Overlay với layout nhiều nhãn trên 1 tờ (multi-up)."],
  820: ["PRT-820", "Chưa thiết lập layout nhãn Header."],
  821: ["PRT-821", "Chưa thiết lập layout nhãn Tail."],
};

export class MlEngineError extends Error {
  constructor(
    readonly code: string,
    readonly fallback: string,
    readonly step: string,
    readonly componentCode: string | null,
  ) {
    super(`${code} (${step}${componentCode === null ? "" : ` componentCode=${componentCode}`})`);
    this.name = "MlEngineError";
  }
}

export function printErrorOf(
  componentCode: string | null,
  step: string,
): { code: string; fallback: string } {
  const hit = componentCode === null ? undefined : PRINT_ERRORS[Number(componentCode)];
  if (hit) return { code: hit[0], fallback: hit[1] };
  return step === "Preview" || step === "PropertyAccess" || step === "CreateInstance"
    ? { code: "PRN-001", fallback: "Không lấy được bản xem trước từ Label Engine. Kiểm tra ứng dụng đã chạy và tên driver máy in." }
    : { code: "PRN-002", fallback: "In qua Label Engine thất bại. Kiểm tra ứng dụng đã chạy và tên driver máy in." };
}

export function printErrorText(e: unknown, t: (k: string, fb: string) => string): string {
  if (e instanceof MlEngineError) return `${e.code} — ${t(e.code, e.fallback)}`;
  if (e instanceof ApiCallError) return `${e.code} — ${errorText(t, e)}`;
  return `PRN-002 — ${t("PRN-002", "Không kết nối được Label Engine. Kiểm tra ứng dụng đã chạy trên máy này.")}`;
}
