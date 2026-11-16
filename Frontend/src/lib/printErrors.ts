import { ApiCallError } from "@/api/types";
import { errorText } from "@/lib/errorText";

const PRINT_ERRORS: Record<string | number, [code: string, fallbackVi: string]> = {
  "-6": ["PRT-N6", "Cannot save the layout file. Check the permissions of the save folder."],
  "-5": ["PRT-N5", "Failed to download the layout file. Check the URL and the certificate settings."],
  "-3": ["PRT-N3", "The specified URL format is invalid."],
  "-2": ["PRT-N2", "Only HTTP or HTTPS URLs can be used."],
  "-1": ["PRT-N1", "The layout file name is invalid."],
  1: ["PRT-1", "Cannot read the layout file. Check that the file exists."],
  3: ["PRT-3", "Connected to the printer."],
  4: ["PRT-4", "Cannot connect to the printer. Check the power, IP address and USB connection."],
  5: ["PRT-5", "The printer connection has not been started."],
  6: ["PRT-6", "Failed to close the printer connection."],
  7: ["PRT-7", "Failed to send data to the printer."],
  8: ["PRT-8", "Sending data to the printer timed out."],
  9: ["PRT-9", "Cannot receive a response from the printer. Check the communication settings."],
  10: ["PRT-10", "No response from the printer."],
  11: ["PRT-11", "The printer has an error. Check the printer status."],
  12: ["PRT-12", "Invalid combination of communication settings and protocol."],
  21: ["PRT-21", "The specified printer driver was not found."],
  22: ["PRT-22", "Failed to delete the print job."],
  51: ["PRT-51", "Cannot read the layout information."],
  52: ["PRT-52", "The printer model specified in the layout is not supported."],
  53: ["PRT-53", "This printer does not support the cutter function."],
  55: ["PRT-55", "This function cannot be used with the specified output method."],
  57: ["PRT-57", "The format or content of the print data is invalid."],
  61: ["PRT-61", "Cannot be used: the layout file version is newer than the system."],
  62: ["PRT-62", "Cannot be used: the Global File version is newer."],
  63: ["PRT-63", "Cannot be used: the Header label file version is newer."],
  64: ["PRT-64", "Cannot be used: the Tail label file version is newer."],
  65: ["PRT-65", "Cannot be used: the Global Table file version is newer."],
  66: ["PRT-66", "Cannot be used: the Global Check Table version is newer."],
  80: ["PRT-80", "The specified client certificate was not found."],
  81: ["PRT-81", "More than one client certificate matches the conditions."],
  100: ["PRT-100", "Cannot read the Global information file."],
  101: ["PRT-101", "Cannot read the Header label file."],
  102: ["PRT-102", "Cannot read the Tail label file."],
  103: ["PRT-103", "The layout settings do not match the Header/Tail label."],
  130: ["PRT-130", "An error occurred while drawing."],
  152: ["PRT-152", "The printer type of the Header label does not match the main layout."],
  153: ["PRT-153", "The printer type of the Tail label does not match the main layout."],
  201: ["PRT-201", "An internal system error occurred. Contact the administrator."],
  202: ["PRT-202", "An internal system error occurred. Contact the administrator."],
  403: ["PRT-403", "No master data matches the entered value."],
  404: ["PRT-404", "No target data exists in the ODBC table."],
  405: ["PRT-405", "Amount calculation failed."],
  406: ["PRT-406", "The data to be calculated is invalid."],
  407: ["PRT-407", "Check digit calculation failed."],
  410: ["PRT-410", "The check digit result is invalid."],
  413: ["PRT-413", "The consumption tax rate is not set."],
  414: ["PRT-414", "No conversion data exists in the Excel table."],
  415: ["PRT-415", "The Application Identifier (AI) format is invalid."],
  430: ["PRT-430", "Condition evaluation failed."],
  431: ["PRT-431", "There is no variable to store the condition result."],
  450: ["PRT-450", "The table referenced in the conditional print settings was not found."],
  470: ["PRT-470", "The format tag is written incorrectly."],
  471: ["PRT-471", "The format tag cannot be used with this variable."],
  472: ["PRT-472", "The format tag cannot be combined with automatic check digit."],
  473: ["PRT-473", "The font width cannot be specified when a format tag is used."],
  500: ["PRT-500", "There is no data to preview."],
  503: ["PRT-503", "Failed to create the preview image."],
  504: ["PRT-504", "Cannot create the folder for preview images."],
  505: ["PRT-505", "Cannot create the preview image."],
  506: ["PRT-506", "Check the disk space or the access permissions."],
  600: ["PRT-600", "The specified font is not installed."],
  601: ["PRT-601", "Cannot get the barcode definition."],
  602: ["PRT-602", "The barcode data is invalid."],
  610: ["PRT-610", "An object is placed outside the print area of the label."],
  611: ["PRT-611", "The print offset moves the print position out of range."],
  700: ["PRT-700", "The Bluetooth device cannot be used."],
  701: ["PRT-701", "The specified Bluetooth device was not found."],
  702: ["PRT-702", "The PIN code is incorrect."],
  703: ["PRT-703", "Bluetooth authentication failed."],
  800: ["PRT-800", "Sequence-number variables cannot be used for LAN/USB/Bluetooth output."],
  801: ["PRT-801", "For direct output, the issue quantity cannot exceed one sheet."],
  802: ["PRT-802", "The issue quantity is not set."],
  803: ["PRT-803", "Form Overlay cannot be used with a multi-up layout."],
  820: ["PRT-820", "The Header label layout is not set."],
  821: ["PRT-821", "The Tail label layout is not set."],
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
    ? { code: "PRN-001", fallback: "Cannot get the preview from the label print engine. Check that it is running and the printer driver name." }
    : { code: "PRN-002", fallback: "Printing through the label print engine failed. Check that it is running and the printer driver name." };
}

export function printErrorText(e: unknown, t: (k: string, fb: string) => string): string {
  if (e instanceof MlEngineError) return `${e.code} — ${t(e.code, e.fallback)}`;
  if (e instanceof ApiCallError) return `${e.code} — ${errorText(t, e)}`;
  return `PRN-003 — ${t("PRN-003", "Cannot connect to the label print engine. Check that it is running on this PC.")}`;
}
