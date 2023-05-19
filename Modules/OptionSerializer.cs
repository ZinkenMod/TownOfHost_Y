using System;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

namespace TownOfHost.Modules;

public static class OptionSerializer
{
    private static LogHandler logger = Logger.Handler(nameof(OptionSerializer));
    private const string Header = "%TOHOptions%", Footer = "%End%";
    public static void SaveToClipboard()
    {
        GUIUtility.systemCopyBuffer = ToString();
        Logger.SendInGame(Utils.ColorString(Color.green, Translator.GetString("Message.CopiedOptions")));
    }
    public static void LoadFromClipboard()
    {
        FromString(GUIUtility.systemCopyBuffer);
    }
    /// <summary>
    /// 現在のMod設定とバニラ設定を<see cref="FromString"/>で読み込める文字列に変換します<br/>
    /// enumは元の整数型に変換します<br/>
    /// 10以上になる可能性のある整数型は，文字数削減のため16進数に変換します<br/>
    /// Mod設定は，プリセットでなく，Valueが0でないもの(データ量削減のため)を書き込みます<br/>
    /// <see cref="Header"/>から始まり，'&amp;'がMod設定とバニラ設定を区切ります<br/>
    /// Mod設定は，'!'が各オプションを区切り，','がオプションIDとオプションの値を区切ります<br/>
    /// [オプション1のID],[オプションの1の値]![オプション2のID],[オプション2の値]!...<br/>
    /// バニラ設定は，'!'が各オプションを区切り，役職オプション以外のオプションは以下のフォーマットです<br/>
    /// [<see cref="OptionType"/>],[オプション名=<see cref="BoolOptionNames"/>など],[オプション値]<br/>
    /// バニラの役職オプションは以下のフォーマットです<br/>
    /// [<see cref="OptionType.RoleRate"/>],[<see cref="RoleTypes"/>],[最大数],[確率]
    /// </summary>
    /// <returns>変換された文字列</returns>
    public new static string ToString()
    {
        var builder = new StringBuilder(Header);
        foreach (var option in OptionItem.AllOptions)
        {
            if (option is PresetOptionItem)
            {
                continue;
            }
            var value = option.GetValue();
            if (value == 0)
            {
                continue;
            }
            builder.Append(option.Id.ToString("x")).Append(",").Append(option.GetValue().ToString("x")).Append("!");
        }

        builder.Append("&");
        var vanillaOptions = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
        foreach (var option in vanillaOptions.AllValues)
        {
            builder.Write(option);
        }
        builder.Append(Footer);
        return builder.ToString();
    }
    /// <summary>
    /// <see cref="ToString"/>で変換された形式の文字列を読み込んで現在のプリセットを上書きします
    /// </summary>
    /// <param name="source">オプション情報の文字列</param>
    public static void FromString(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            logger.Info("文字列が空");
            goto Failed;
        }
        if (!source.StartsWith(Header))
        {
            logger.Info("ヘッダがありません");
            goto Failed;
        }
        // ヘッダを削除
        source = source.Replace(Header, "");
        var footerAt = source.IndexOf(Footer);
        if (footerAt < 0)
        {
            logger.Info("フッタがありません");
            goto Failed;
        }
        // フッタ以降を削除
        source = source[..footerAt];

        foreach (var option in OptionItem.AllOptions)
        {
            if (option is PresetOptionItem)
            {
                continue;
            }
            option.SetValue(0);
        }

        try
        {

            var entries = source.Split('&');

            var modOptions = entries[0].Split('!', StringSplitOptions.RemoveEmptyEntries);
            foreach (var hexOption in modOptions)
            {
                var split = hexOption.Split(',');
                var id = HexToInt32(split[0]);
                var value = HexToInt32(split[1]);

                var option = OptionItem.AllOptions.FirstOrDefault(option => option.Id == id);
                if (option != null)
                {
                    option.SetValue(value);
                }
            }

            var vanillaOptions = entries[1].Split('!', StringSplitOptions.RemoveEmptyEntries);
            foreach (var vanillaOption in vanillaOptions)
            {
                var split = vanillaOption.Split(',');
                split.Read();
            }

            Logger.SendInGame(Utils.ColorString(Color.green, Translator.GetString("Message.LoadedOptions")));
        }
        catch (Exception ex)
        {
            logger.Exception(ex);
            goto Failed;
        }
        return;

    Failed:
        Logger.SendInGame(Translator.GetString("Message.FailedToLoadOptions"));
    }
    private static StringBuilder Write(this StringBuilder builder, OptionBackupValue option)
    {
        switch (option)
        {
            case ByteOptionBackupValue byteOption: builder.WriteByte(byteOption); break;
            case BoolOptionBackupValue boolOption: builder.WriteBool(boolOption); break;
            // floatは10進
            case FloatOptionBackupValue floatOption: builder.WriteFloat(floatOption); break;
            case IntOptionBackupValue intOption: builder.WriteInt(intOption); break;
            case UIntOptionBackupValue uIntOption: builder.WriteUInt(uIntOption); break;
            case RoleRateBackupValue roleRate: builder.WriteRoleRate(roleRate); break;
            default: logger.Warn("不明なオプションの書き込み"); break;
        }
        builder.Append("!");
        return builder;
    }
    private static StringBuilder WriteByte(this StringBuilder builder, ByteOptionBackupValue byteOption) =>
        builder.Append((int)OptionType.Byte).Append(",").Append(((int)byteOption.OptionName).ToString("x")).Append(",").Append(byteOption.Value.ToString("x"));
    private static StringBuilder WriteBool(this StringBuilder builder, BoolOptionBackupValue boolOption) =>
        builder.Append((int)OptionType.Bool).Append(",").Append(((int)boolOption.OptionName).ToString("x")).Append(",").Append(Convert.ToInt32(boolOption.Value));
    private static StringBuilder WriteFloat(this StringBuilder builder, FloatOptionBackupValue floatOption) =>
        builder.Append((int)OptionType.Float).Append(",").Append(((int)floatOption.OptionName).ToString("x")).Append(",").Append(floatOption.Value);
    private static StringBuilder WriteInt(this StringBuilder builder, IntOptionBackupValue intOption) =>
        builder.Append((int)OptionType.Int).Append(",").Append(((int)intOption.OptionName).ToString("x")).Append(",").Append(intOption.Value.ToString("x"));
    private static StringBuilder WriteUInt(this StringBuilder builder, UIntOptionBackupValue uIntOption) =>
        builder.Append((int)OptionType.UInt).Append(",").Append(((int)uIntOption.OptionName).ToString("x")).Append(",").Append(uIntOption.Value.ToString("x"));
    private static StringBuilder WriteRoleRate(this StringBuilder builder, RoleRateBackupValue roleRate) =>
        builder.Append((int)OptionType.RoleRate).Append(",").Append((ushort)roleRate.roleType).Append(",").Append(roleRate.maxCount.ToString("x")).Append(",").Append(roleRate.chance.ToString("x"));
    private static void Read(this string[] args)
    {
        var optionType = (OptionType)Convert.ToInt32(args[0]);
        switch (optionType)
        {
            case OptionType.Byte: ReadByte(args); break;
            case OptionType.Bool: ReadBool(args); break;
            case OptionType.Float: ReadFloat(args); break;
            case OptionType.Int: ReadInt(args); break;
            case OptionType.UInt: ReadUInt(args); break;
            case OptionType.RoleRate: ReadRoleRate(args); break;
            default: logger.Warn($"不明なオプションタイプの読み込み: {optionType}"); break;
        }
    }
    private static void ReadByte(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.SetByte((ByteOptionNames)HexToInt32(args[1]), HexToByte(args[2]));
    private static void ReadBool(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.SetBool((BoolOptionNames)HexToInt32(args[1]), Convert.ToInt32(args[2]) > 0);
    private static void ReadFloat(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.SetFloat((FloatOptionNames)HexToInt32(args[1]), Convert.ToSingle(args[2]));
    private static void ReadInt(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.SetInt((Int32OptionNames)HexToInt32(args[1]), HexToInt32(args[2]));
    private static void ReadUInt(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.SetUInt((UInt32OptionNames)HexToInt32(args[1]), HexToUInt32(args[2]));
    private static void ReadRoleRate(string[] args) =>
        GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.SetRoleRate((RoleTypes)Convert.ToUInt16(args[1]), HexToInt32(args[2]), HexToInt32(args[3]));

    private static int HexToInt32(string hex) => Convert.ToInt32(hex, 16);
    private static uint HexToUInt32(string hex) => Convert.ToUInt32(hex, 16);
    private static byte HexToByte(string hex) => Convert.ToByte(hex, 16);

    private enum OptionType { Byte, Bool, Float, Int, UInt, RoleRate, }
}