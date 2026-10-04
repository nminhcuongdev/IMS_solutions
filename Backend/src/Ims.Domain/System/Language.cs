using Ims.Domain.Common;

namespace Ims.Domain.System;

public class Language : SoftDeletableEntity
{
    public string LanguageCode { get; set; } = null!;
    public string LanguageName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
