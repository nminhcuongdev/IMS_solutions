import { apiCall } from "./client";

export interface LanguageOption {
  languageCode: string;
  languageName: string;
  iconImage: string | null;
}

export const getLanguages = () => apiCall<LanguageOption[]>("/languages");

export const getLabels = (lang: string, deviceScope = "PC") =>
  apiCall<Record<string, string>>("/language-data/labels", {
    query: { lang, deviceScope },
  });
