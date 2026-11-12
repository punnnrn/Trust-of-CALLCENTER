/// มินิเกมที่มีคูลดาวน์ implement อันนี้ เพื่อให้ CooldownBadge / CooldownWatcher อ่านสถานะได้
public interface ICooldownProvider
{
    bool IsCoolingDown { get; }
    /// เวลาที่เหลือ หน่วยเป็น 'วินาทีในเกม' (60 = 1 นาทีในเกม)
    int CooldownRemainingGameSeconds { get; }
}
