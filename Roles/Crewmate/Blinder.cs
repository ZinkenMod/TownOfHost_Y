using System.Collections.Generic;

using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Blinder : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Blinder),
            player => new Blinder(player),
            CustomRoles.Blinder,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            36000,
            SetupOptionItem,
            "ブラインダー",
            "#883fd1"
        );
    public Blinder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BlinderVision = OptionBlinderVision.GetFloat();

        BlindPlayer = new();
    }

    private static OptionItem OptionBlinderVision;
    enum OptionName
    {
        BlinderVision,
    }

    public static float BlinderVision;
    public static List<byte> BlindPlayer = new();

    private static void SetupOptionItem()
    {
        OptionBlinderVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.BlinderVision, new(0f, 5f, 0.05f), 0.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add()
    {
        BlindPlayer.Clear();
    }
    public void SendRPC(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.SetBlinderVisionPlayer);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetBlinderVisionPlayer) return;

        BlindPlayer.Add(reader.ReadByte());
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetFloat(FloatOptionNames.CrewLightMod, BlinderVision);
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        BlindPlayer.Add(killer.PlayerId);
        SendRPC(killer.PlayerId);
        killer.MarkDirtySettings();
        return;
    }
}