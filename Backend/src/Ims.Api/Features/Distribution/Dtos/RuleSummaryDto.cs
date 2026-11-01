namespace Ims.Api.Features.Distribution;

public sealed record RuleSummaryDto(int Targets, int Latest, int Downloaded, int Waiting, int Attention, int Unconfirmed);
