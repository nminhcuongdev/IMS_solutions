namespace Ims.Domain.Masters;

public enum ItemGroupCategory : byte
{
    FinishedGoods = 1,
    SemiFinished = 2,
    WorkInProgress = 3,
    RawMaterials = 4,
    PurchasedParts = 5,
    OutsourcedProcessing = 6,
    AuxiliaryMaterials = 7
}
