using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Impostor;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Potentialist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Potentialist),
            player => new Potentialist(player),
            CustomRoles.Potentialist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            49900,
            null,
            "ポテンシャリスト"
        );
    public Potentialist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        CanChangeNeutral = OptionCanChangeNeutral.GetBool();
    }

    private static OptionItem OptionTaskTrigger; //効果を発動するタスク完了数
    private static OptionItem OptionCanChangeNeutral;
    enum OptionName
    {
        poTask,
        poNeutral
    }
    private static int TaskTrigger;
    private static bool CanChangeNeutral;

    bool isPotentialistChanged;

    // 直接設置
    public static void SetupRoleOptions()
    {
        TextOptionItem.Create(40, "Head.LimitedTimeRole", TabGroup.CrewmateRoles)
            .SetColor(Color.yellow);
        var spawnOption = IntegerOptionItem.Create(RoleInfo.ConfigId, "PotentialistName", new(0, 100, 10), 0, TabGroup.CrewmateRoles, false)
            .SetColor(RoleInfo.RoleColor)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;
        var countOption = IntegerOptionItem.Create(RoleInfo.ConfigId + 1, "Maximum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.Standard);

        Options.CustomRoleSpawnChances.Add(RoleInfo.RoleName, spawnOption);
        Options.CustomRoleCounts.Add(RoleInfo.RoleName, countOption);

        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, OptionName.poTask, new(1, 30, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCanChangeNeutral = BooleanOptionItem.Create(RoleInfo, 11, OptionName.poNeutral, false, false);
    }

    public override void Add()
    {
        isPotentialistChanged = false;
    }
    public override bool OnCompleteTask()
    {
        var playerId = Player.PlayerId;
        if (Player.IsAlive()
            && !isPotentialistChanged
            && MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger))
        {   //生きていて、変更済みでなく、全タスク完了orトリガー数までタスクを完了している場合
            var rand = IRandom.Instance;
            List<CustomRoles> Rand = new()
                {
                    CustomRoles.Madmate,
                    CustomRoles.MadDictator,
                    CustomRoles.MadNimrod,
                    CustomRoles.NiceWatcher,
                    CustomRoles.Bait,
                    CustomRoles.Lighter,
                    CustomRoles.Mayor,
                    CustomRoles.SabotageMaster,
                    CustomRoles.Snitch,
                    CustomRoles.SpeedBooster,
                    CustomRoles.Doctor,
                    CustomRoles.Trapper,
                    CustomRoles.Dictator,
                    CustomRoles.Seer,
                    CustomRoles.TimeManager,
                    CustomRoles.Bakery,
                    CustomRoles.TaskManager,
                    CustomRoles.Nekomata,
                    CustomRoles.Express,
                    CustomRoles.SeeingOff,
                    CustomRoles.Rainbow,
                    CustomRoles.Blinder,
                    CustomRoles.CandleLighter,
                    CustomRoles.FortuneTeller,
                    CustomRoles.Nimrod,
                    CustomRoles.Detector,
                };

            if (CanChangeNeutral)
            {
                Rand.Add(CustomRoles.Jester);
                Rand.Add(CustomRoles.Opportunist);
                Rand.Add(CustomRoles.Terrorist);
                Rand.Add(CustomRoles.SchrodingerCat);
                Rand.Add(CustomRoles.AntiComplete);
                Rand.Add(CustomRoles.LoveCutter);
            }
            var Role = Rand[rand.Next(Rand.Count)];
            Player.RpcSetCustomRole(Role);

            isPotentialistChanged = true;
            Logger.Info(Player.GetRealName() + " 役職変更先:" + Role, "Potentialist");
        }
        return true;
    }

    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        roleText = Utils.GetRoleName(CustomRoles.Crewmate);
        roleColor = Utils.GetRoleColor(CustomRoles.Crewmate);
    }
}

public static class SpecialEvent
{
    public static bool IsEventRole(CustomRoles role) => role is CustomRoles.Potentialist or CustomRoles.EvilHacker;

    public static void SetupOptions()
    {
        if (Main.IsInitialRelease)
        {
            if (CultureInfo.CurrentCulture.Name == "ja-JP")
            {
                EvilHacker.SetupRoleOptions();
                EvilHacker.RoleInfo.OptionCreator?.Invoke();
            }
            Potentialist.SetupRoleOptions();
            Potentialist.RoleInfo.OptionCreator?.Invoke();
        }
    }

    public static string TitleText() =>
        "\n<size=50%>TOH_Yは一周年を迎えました！" +
        "\n周年記念として、11/15まで期間限定役職が登場★" +
        "\nパン屋が転職「おにぎり屋」(バレンタイン/ホワイトデー復刻)" +
        "\n途中で役職変化!「ポテンシャリスト」(エイプリルフール復刻)" +
        "\nインポスターも強くなりたい「イビルハッカー...?」新規登場" +
        "\nこれからもTOH_Yをよろしくお願いします！</size>";
}