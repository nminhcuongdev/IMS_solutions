import { apiCall } from "./client";

export interface LanguageSlot {
  slot: number;
  languageCode: string;
  languageName: string;
  flagIcon: string | null;
}

export const getLanguageSlots = () => apiCall<LanguageSlot[]>("/languages");

export const getLabels = (lang: string, deviceScope = "PC") =>
  apiCall<Record<string, string>>("/language-data/labels", {
    query: { lang, deviceScope },
  });
