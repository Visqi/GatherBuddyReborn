﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using GatherBuddy.Config;
using GatherBuddy.Enums;
using GatherBuddy.FishTimer;
using GatherBuddy.Plugin;
using ImGuiNET;
using OtterGui;
using OtterGui.Table;
using Newtonsoft.Json;
using ImRaii = OtterGui.Raii.ImRaii;
using System.Text;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using GatherBuddy.Models;
using GatherBuddy.Time;
using GatherBuddy.Weather;
using OtterGui.Text;

namespace GatherBuddy.Gui;

public partial class Interface
{
    private sealed class RecordTable : Table<FishRecord>
    {
        public const string FileNamePopup = "FileNamePopup";

        public RecordTable()
            : base("Fish Records", _plugin.FishRecorder.Records, _catchHeader, _baitHeader, _durationHeader, _castStartHeader,
                _biteTypeHeader, _hookHeader, _amountHeader, _spotHeader, _contentIdHeader, _gatheringHeader, _perceptionHeader, _sizeHeader,
                _flagHeader)
            => Flags |= ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable;

        private        int _lastCount;
        private static int _deleteIdx = -1;

        protected override void PreDraw()
        {
            ExtraHeight = ImGui.GetFrameHeightWithSpacing() / ImGuiHelpers.GlobalScale;
            if (_deleteIdx > -1)
            {
                _plugin.FishRecorder.Remove(_deleteIdx);
                _deleteIdx = -1;
            }

            if (_lastCount != Items.Count)
            {
                FilterDirty = true;
                _lastCount  = Items.Count;
            }
        }

        private static readonly ContentIdHeader  _contentIdHeader  = new() { Label = "Content ID" };
        private static readonly BaitHeader       _baitHeader       = new() { Label = "Bait" };
        private static readonly SpotHeader       _spotHeader       = new() { Label = "Fishing Spot" };
        private static readonly CatchHeader      _catchHeader      = new() { Label = "Caught Fish" };
        private static readonly CastStartHeader  _castStartHeader  = new() { Label = "TimeStamp" };
        private static readonly BiteTypeHeader   _biteTypeHeader   = new() { Label = "Tug" };
        private static readonly HookHeader       _hookHeader       = new() { Label = "Hookset" };
        private static readonly DurationHeader   _durationHeader   = new() { Label = "Bite" };
        private static readonly GatheringHeader  _gatheringHeader  = new() { Label = "Gath." };
        private static readonly PerceptionHeader _perceptionHeader = new() { Label = "Perc." };
        private static readonly AmountHeader     _amountHeader     = new() { Label = "Amt" };
        private static readonly SizeHeader       _sizeHeader       = new() { Label = "Ilm" };
        private static readonly FlagHeader       _flagHeader       = new() { Label = "Flags" };

        private sealed class GatheringHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
                => record.Gathering.ToString();

            public override float Width
                => 50 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Gathering.CompareTo(rhs.Gathering);

            public override void DrawColumn(FishRecord record, int _)
                => ImGuiUtil.RightAlign(ToName(record));
        }

        private sealed class PerceptionHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
                => record.Perception.ToString();

            public override float Width
                => 50 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Perception.CompareTo(rhs.Gathering);

            public override void DrawColumn(FishRecord record, int _)
                => ImGuiUtil.RightAlign(ToName(record));
        }

        private sealed class AmountHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
                => record.Amount.ToString();

            public override float Width
                => 35 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Amount.CompareTo(rhs.Amount);

            public override void DrawColumn(FishRecord record, int _)
            {
                ImGuiUtil.RightAlign(ToName(record));
            }
        }

        private sealed class SizeHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
                => $"{record.Size / 10f:F1}";

            public override float Width
                => 50 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Size.CompareTo(rhs.Size);

            public override void DrawColumn(FishRecord record, int _)
            {
                var tt = string.Empty;
                if (record.Flags.HasFlag(Effects.Large))
                    tt = "Large Catch!";
                if (record.Flags.HasFlag(Effects.Collectible))
                    tt += tt.Length > 0 ? "\nCollectible!" : "Collectible!";
                using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.DisabledText.Value(), tt.Length == 0);
                ImGuiUtil.RightAlign(ToName(record));
                ImGuiUtil.HoverTooltip(tt);
            }
        }


        private sealed class ContentIdHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord item)
                => item.Flags.HasFlag(Effects.Legacy) ? "Legacy" : item.ContentIdHash.ToString("X8");

            public override float Width
                => 75 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.ContentIdHash.CompareTo(rhs.ContentIdHash);
        }

        private sealed class BaitHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord item)
                => item.Bait.Name;

            public override float Width
                => 150 * ImGuiHelpers.GlobalScale;
        }

        private sealed class SpotHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord item)
                => item.FishingSpot?.Name ?? "Unknown";

            public override float Width
                => 200 * ImGuiHelpers.GlobalScale;
        }

        private sealed class CatchHeader : ColumnString<FishRecord>
        {
            public CatchHeader()
            {
                Flags |= ImGuiTableColumnFlags.NoHide;
                Flags |= ImGuiTableColumnFlags.NoReorder;
            }

            public override string ToName(FishRecord record)
                => record.Catch?.Name[GatherBuddy.Language] ?? "None";

            public override float Width
                => 200 * ImGuiHelpers.GlobalScale;

            public override void DrawColumn(FishRecord record, int idx)
            {
                base.DrawColumn(record, idx);
                if (ImGui.GetIO().KeyCtrl && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    _deleteIdx = idx;
                ImGuiUtil.HoverTooltip("Hold Control and right-click to delete...");
            }
        }

        private sealed class CastStartHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
            {
                if (!GatherBuddy.Config.UseUnixTimeFishRecords)
                    return (record.TimeStamp.Time / 1000).ToString();

                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(record.TimeStamp.Time).ToLocalTime();
                return dateTime.ToString("g");
            }

            public override float Width
                => 80 * ImGuiHelpers.GlobalScale;

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.TimeStamp.CompareTo(rhs.TimeStamp);

            public override void DrawColumn(FishRecord record, int _)
            {
                base.DrawColumn(record, _);
                ImGuiUtil.HoverTooltip(record.TimeStamp.ToString());
            }
        }

        [Flags]
        private enum TugTypeFilter : byte
        {
            Weak      = 0x01,
            Strong    = 0x02,
            Legendary = 0x04,
            Unknown   = 0x08,
            None      = 0x10,
        }

        private sealed class BiteTypeHeader : ColumnFlags<TugTypeFilter, FishRecord>
        {
            public BiteTypeHeader()
            {
                AllFlags = TugTypeFilter.Weak | TugTypeFilter.Strong | TugTypeFilter.Legendary | TugTypeFilter.Unknown | TugTypeFilter.None;
                _filter  = AllFlags;
            }

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Tug.CompareTo(rhs.Tug);

            public override void DrawColumn(FishRecord item, int idx)
                => ImGui.Text(item.Tug.ToString());

            private TugTypeFilter _filter;

            protected override void SetValue(TugTypeFilter value, bool enable)
            {
                if (enable)
                    _filter |= value;
                else
                    _filter &= ~value;
            }

            public override TugTypeFilter FilterValue
                => _filter;

            public override bool FilterFunc(FishRecord item)
                => item.Tug switch
                {
                    BiteType.Weak      => _filter.HasFlag(TugTypeFilter.Weak),
                    BiteType.Strong    => _filter.HasFlag(TugTypeFilter.Strong),
                    BiteType.Legendary => _filter.HasFlag(TugTypeFilter.Legendary),
                    BiteType.None      => _filter.HasFlag(TugTypeFilter.None),
                    _                  => _filter.HasFlag(TugTypeFilter.Unknown),
                };

            public override float Width
                => 60 * ImGuiHelpers.GlobalScale;
        }

        [Flags]
        private enum HookSetFilter : byte
        {
            Regular  = 0x01,
            Precise  = 0x02,
            Powerful = 0x04,
            Double   = 0x08,
            Triple   = 0x10,
            Unknown  = 0x20,
            Stellar  = 0x40,
            None     = 0x80,
        }

        private sealed class HookHeader : ColumnFlags<HookSetFilter, FishRecord>
        {
            public HookHeader()
            {
                AllFlags = HookSetFilter.Precise
                  | HookSetFilter.Powerful
                  | HookSetFilter.Regular
                  | HookSetFilter.Double
                  | HookSetFilter.Triple
                  | HookSetFilter.Stellar
                  | HookSetFilter.Unknown
                  | HookSetFilter.None;
                _filter = AllFlags;
            }

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Hook.CompareTo(rhs.Hook);

            public override void DrawColumn(FishRecord item, int idx)
                => ImGui.Text(item.Hook.ToName());

            private HookSetFilter _filter;

            protected override void SetValue(HookSetFilter value, bool enable)
            {
                if (enable)
                    _filter |= value;
                else
                    _filter &= ~value;
            }

            public override HookSetFilter FilterValue
                => _filter;

            public override bool FilterFunc(FishRecord item)
                => item.Hook switch
                {
                    HookSet.Precise    => _filter.HasFlag(HookSetFilter.Precise),
                    HookSet.Powerful   => _filter.HasFlag(HookSetFilter.Powerful),
                    HookSet.Hook       => _filter.HasFlag(HookSetFilter.Regular),
                    HookSet.DoubleHook => _filter.HasFlag(HookSetFilter.Double),
                    HookSet.TripleHook => _filter.HasFlag(HookSetFilter.Triple),
                    HookSet.Stellar    => _filter.HasFlag(HookSetFilter.Stellar),
                    HookSet.None       => _filter.HasFlag(HookSetFilter.None),
                    _                  => _filter.HasFlag(HookSetFilter.Unknown),
                };

            public override float Width
                => 75 * ImGuiHelpers.GlobalScale;
        }

        private sealed class DurationHeader : ColumnString<FishRecord>
        {
            public override string ToName(FishRecord record)
                => $"{record.Bite / 1000}.{record.Bite % 1000:D3}";

            public override float Width
                => 50 * ImGuiHelpers.GlobalScale;

            public override void DrawColumn(FishRecord record, int _)
                => ImGuiUtil.RightAlign(ToName(record));

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Bite.CompareTo(rhs.Bite);
        }

        private class FlagHeader : TriStateColumnFlags<FlagHeader.ColumnEffects, FishRecord>
        {
            private          float                                           _iconScale;
            private readonly (ISharedImmediateTexture, Effects)[] _effects;

            [Flags]
            public enum ColumnEffects : ulong
            {
                LargeCatch        = Effects.Large,
                AverageCatch      = (ulong)Effects.Large << 32,
                CollectibleOn     = Effects.Collectible,
                CollectibleOff    = (ulong)Effects.Collectible << 32,
                PatienceOn        = Effects.Patience,
                PatienceOff       = (ulong)Effects.Patience << 32,
                Patience2On       = Effects.Patience2,
                Patience2Off      = (ulong)Effects.Patience2 << 32,
                IntuitionOn       = Effects.Intuition,
                IntuitionOff      = (ulong)Effects.Intuition << 32,
                SnaggingOn        = Effects.Snagging,
                SnaggingOff       = (ulong)Effects.Snagging << 32,
                FishEyesOn        = Effects.FishEyes,
                FishEyesOff       = (ulong)Effects.FishEyes << 32,
                ChumOn            = Effects.Chum,
                ChumOff           = (ulong)Effects.Chum << 32,
                PrizeCatchOn      = Effects.PrizeCatch,
                PrizeCatchOff     = (ulong)Effects.PrizeCatch << 32,
                IdenticalCastOn   = Effects.IdenticalCast,
                IdenticalCastOff  = (ulong)Effects.IdenticalCast << 32,
                SurfaceSlapOn     = Effects.SurfaceSlap,
                SurfaceSlapOff    = (ulong)Effects.SurfaceSlap << 32,
                BigGameFishingOn  = Effects.BigGameFishing,
                BigGameFishingOff = (ulong)Effects.BigGameFishing << 32,
                AmbitiousLureOn   = Effects.AmbitiousLure1 | Effects.AmbitiousLure2,
                AmbitiousLureOff  = (ulong)(Effects.AmbitiousLure1 | Effects.AmbitiousLure2) << 32,
                ModestLureOn      = Effects.ModestLure1 | Effects.ModestLure2,
                ModestLureOff     = (ulong)(Effects.ModestLure1 | Effects.ModestLure2) << 32,
            }

            private static readonly ColumnEffects Mask = Enum.GetValues<ColumnEffects>().Aggregate((a, b) => a | b);

            private static readonly (ColumnEffects On, ColumnEffects Off)[] _values =
            [
                (ColumnEffects.LargeCatch, ColumnEffects.AverageCatch),
                (ColumnEffects.CollectibleOn, ColumnEffects.CollectibleOff),
                (ColumnEffects.PatienceOn, ColumnEffects.PatienceOff),
                (ColumnEffects.Patience2On, ColumnEffects.Patience2Off),
                (ColumnEffects.IntuitionOn, ColumnEffects.IntuitionOff),
                (ColumnEffects.SnaggingOn, ColumnEffects.SnaggingOff),
                (ColumnEffects.FishEyesOn, ColumnEffects.FishEyesOff),
                (ColumnEffects.ChumOn, ColumnEffects.ChumOff),
                (ColumnEffects.PrizeCatchOn, ColumnEffects.PrizeCatchOff),
                (ColumnEffects.IdenticalCastOn, ColumnEffects.IdenticalCastOff),
                (ColumnEffects.SurfaceSlapOn, ColumnEffects.SurfaceSlapOff),
                (ColumnEffects.BigGameFishingOn, ColumnEffects.BigGameFishingOff),
                (ColumnEffects.AmbitiousLureOn, ColumnEffects.AmbitiousLureOff),
                (ColumnEffects.ModestLureOn, ColumnEffects.ModestLureOff),
            ];

            private static readonly string[] _names =
            [
                "Large Catch",
                "Collectible",
                "Patience",
                "Patience II",
                "Intuition",
                "Snagging",
                "Fish Eyes",
                "Chum",
                "Prize Catch",
                "Identical Cast",
                "Surface Slap",
                "Big Game Fishing",
                "Ambitious Lure",
                "Modest Lure",
            ];

            protected override IReadOnlyList<(ColumnEffects On, ColumnEffects Off)> Values
                => _values;

            protected override string[] Names
                => _names;

            protected override void SetValue(ColumnEffects value, bool enable)
            {
                if (enable)
                    _filter |= value;
                else
                    _filter &= ~value;
            }

            protected override void SetValue((ColumnEffects On, ColumnEffects Off) value, bool? enable)
            {
                switch (enable)
                {
                    case null:  _filter |= value.On | value.Off; break;
                    case true:  _filter =  (_filter | value.On) & ~value.Off; break;
                    case false: _filter =  (_filter | value.Off) & ~value.On; break;
                }
            }

            private ColumnEffects _filter;

            public FlagHeader()
            {
                _effects =
                [
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211112), (Effects)_values[00].On), // Nature's Bounty
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211008), (Effects)_values[01].On), // Collector's Glove
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(216023), (Effects)_values[02].On), // Patience
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211106), (Effects)_values[03].On), // Patience II
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211101), (Effects)_values[04].On), // Intuition
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211102), (Effects)_values[05].On), // Snagging
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211103), (Effects)_values[06].On), // Fish Eyes
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211104), (Effects)_values[07].On), // Chum
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211119), (Effects)_values[08].On), // Prize Catch
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211116), (Effects)_values[09].On), // Identical Cast
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211115), (Effects)_values[10].On), // Surface Slap
                    (Icons.DefaultStorage.TextureProvider.GetFromGameIcon(211122), (Effects)_values[11].On), // Big Game Fishing
                ];
                AllFlags   =  Mask;
                _filter    =  AllFlags;
                ComboFlags |= ImGuiComboFlags.HeightLarge;
            }

            public override float Width
            {
                get
                {
                    if (_iconScale == 0)
                    {
                        var scale = _effects[0].Item1.TryGetWrap(out var wrap, out _) ? (float)wrap.Width / wrap.Height : 0;
                        if (scale == 0)
                            return 10 * (TextHeight + 1);

                        _iconScale = scale;
                    }

                    return 14 * (_iconScale * TextHeight + 1);
                }
            }

            public override bool FilterFunc(FishRecord item)
            {
                var enabled  = (Effects)(_filter & Mask);
                var disabled = (Effects)(((ulong)_filter >> 32) & (ulong)Mask);
                var flags    = item.Flags & (Effects)Mask;
                var invFlags = ~flags & (Effects)Mask;
                return (flags & enabled) == flags && (invFlags & disabled) == invFlags;
            }

            public override int Compare(FishRecord lhs, FishRecord rhs)
                => lhs.Flags.CompareTo(rhs.Flags);

            public override ColumnEffects FilterValue
                => _filter;

            private void DrawIcon(FishRecord item, ISharedImmediateTexture icon, Effects flag)
                => DrawIcon(icon, item.Flags.HasFlag(flag), flag.ToString());

            private void DrawIcon(ISharedImmediateTexture icon, bool enabled, string tooltip)
            {
                var size = new Vector2(TextHeight * _iconScale, TextHeight);
                var tint = enabled ? Vector4.One : new Vector4(0.75f, 0.75f, 0.75f, 0.5f);
                if (!icon.TryGetWrap(out var wrap, out _))
                {
                    ImGui.Dummy(size);
                    return;
                }

                ImGui.Image(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, tint);
                if (!ImGui.IsItemHovered())
                    return;

                using var tt = ImRaii.Tooltip();
                ImGui.Image(wrap.ImGuiHandle, new Vector2(wrap.Width, wrap.Height));
                ImUtf8.Text(tooltip);
            }

            public override void DrawColumn(FishRecord item, int idx)
            {
                using var space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One);
                foreach (var (icon, flag) in _effects)
                {
                    DrawIcon(item, icon, flag);
                    ImGui.SameLine();
                }

                switch (item.Flags.AmbitiousLure())
                {
                    case 0:
                        DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218905), false, "Ambitious Lure");
                        ImGui.SameLine();
                        switch (item.Flags.ModestLure())
                        {
                            case 0: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218909), false, "Modest Lure"); break;
                            case 1: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218909), true,  "Modest Lure"); break;
                            case 2: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218910), true,  "Modest Lure"); break;
                            case 3: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218911), true,  "Modest Lure"); break;
                        }

                        return;
                    case 1: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218905), true, "Ambitious Lure"); break;
                    case 2: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218906), true, "Ambitious Lure"); break;
                    case 3: DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218907), true, "Ambitious Lure"); break;
                }

                ImGui.SameLine();
                DrawIcon(Icons.DefaultStorage.TextureProvider.GetFromGameIcon(218909), false, "Modest Lure");
            }
        }

        public string CreateTsv()
        {
            var sb = new StringBuilder(Items.Count * 128);
            sb.Append(
                "Fish\tFishId\tBite\tBait\tBaitId\tSpot\tSpotId\tTug\tHookset\tTimestamp\tEorzea Time\tTransition\tWeather\tAmount\tIlm\tGathering\tPerception\tPatience\tPatience2\tIntuition\tSnagging\tFish Eyes\tChum\tPrize Catch\tIdentical Cast\tSurface Slap\tCollectible\tBig Game Fishing\tAmbitious Lure\tModest Lure\n");
            foreach (var record in Items.OrderBy(r => r.TimeStamp))
            {
                var (hour, minute) = record.TimeStamp.CurrentEorzeaTimeOfDay();
                var spot = record.FishingSpot;
                var (weather, transition) = ("Unknown", "Unknown");
                if (spot != null)
                {
                    var weathers = WeatherManager.GetForecast(spot.Territory, 2, record.TimeStamp.AddEorzeaHours(-8));
                    transition = weathers[0].Weather.Name;
                    weather    = weathers[1].Weather.Name;
                }

                sb.Append(_catchHeader.ToName(record)).Append('\t')
                    .Append(record.CatchId).Append('\t')
                    .Append(_durationHeader.ToName(record)).Append('\t')
                    .Append(_baitHeader.ToName(record)).Append('\t')
                    .Append(record.BaitId).Append('\t')
                    .Append(_spotHeader.ToName(record)).Append('\t')
                    .Append(record.SpotId).Append('\t')
                    .Append(record.Tug.ToString()).Append('\t')
                    .Append(record.Hook.ToString()).Append('\t')
                    .Append(_castStartHeader.ToName(record)).Append('\t')
                    .Append($"{hour}:{minute:D2}").Append('\t')
                    .Append(transition).Append('\t')
                    .Append(weather).Append('\t')
                    .Append(_amountHeader.ToName(record)).Append('\t')
                    .Append(_sizeHeader.ToName(record)).Append('\t')
                    .Append(_gatheringHeader.ToName(record)).Append('\t')
                    .Append(_perceptionHeader.ToName(record)).Append('\t')
                    .Append(record.Flags.HasFlag(Effects.Patience) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.Patience2) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.Intuition) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.Snagging) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.FishEyes) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.Chum) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.PrizeCatch) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.IdenticalCast) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.SurfaceSlap) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.Collectible) ? "x\t" : "\t")
                    .Append(record.Flags.HasFlag(Effects.BigGameFishing) ? "x\t" : "\t")
                    .Append($"{record.Flags.AmbitiousLure()}\t")
                    .Append($"{record.Flags.ModestLure()}\t")
                    .Append('\n');
            }

            return sb.ToString();
        }
    }

    private readonly RecordTable _recordTable;
    private          bool        WriteTsv  = false;
    private          bool        WriteJson = false;


    private void DrawRecordTab()
    {
        using var id  = ImUtf8.PushId("Fish Records"u8);
        using var tab = ImUtf8.TabItem("Fish Records"u8);
        ImUtf8.HoverTooltip("The records of my fishing prowess have been greatly exaggerated.\n"u8
          + "Find, cleanup and share all data you have collected while fishing."u8);
        if (!tab)
            return;

        _recordTable.Draw(ImGui.GetTextLineHeightWithSpacing());

        var textSize = ImUtf8.CalcTextSize("00000000"u8) with { Y = 0 };
        if (_recordTable.CurrentItems != _recordTable.TotalItems)
            ImGuiUtil.DrawTextButton($"{_recordTable.CurrentItems}", textSize, ImGui.GetColorU32(ImGuiCol.Button),
                ColorId.AvailableItem.Value());
        else
            ImGuiUtil.DrawTextButton($"{_recordTable.CurrentItems}", textSize, ImGui.GetColorU32(ImGuiCol.Button));
        ImGui.SameLine();
        if (ImUtf8.Button("Cleanup"u8))
        {
            _plugin.FishRecorder.RemoveDuplicates();
            _plugin.FishRecorder.RemoveInvalid();
        }

        ImUtf8.HoverTooltip("Delete all entries that were marked as invalid for some reason,\n"u8
          + "as well as all entries that have a duplicate (with the same content id and timestamp).\n"u8
          + "Usually, there should be none such entries.\n"u8
          + "Use at your own risk, no backup will be created automatically."u8);

        ImGui.SameLine();
        try
        {
            if (ImUtf8.Button("Copy to Clipboard"u8))
                ImGui.SetClipboardText(_plugin.FishRecorder.ExportBase64());
            ImUtf8.HoverTooltip("Export all fish records to your clipboard, to share them with other people. This may be a lot"u8);
        }
        catch
        {
            // ignored
        }

        ImGui.SameLine();
        try
        {
            if (ImUtf8.Button("Import from Clipboard"u8))
                _plugin.FishRecorder.ImportBase64(ImGui.GetClipboardText());
            ImUtf8.HoverTooltip("Import a set of fish records shared with you from your clipboard. Should automatically skip duplicates."u8);
        }
        catch
        {
            // ignored
        }

        ImGui.SameLine();
        try
        {
            if (ImUtf8.Button("Export JSON"u8))
            {
                ImGui.OpenPopup(RecordTable.FileNamePopup);
                WriteJson = true;
            }

            ImUtf8.HoverTooltip("Given a path, export all records as a single JSON file."u8);
        }
        catch
        {
            // ignored
        }

        ImGui.SameLine();
        try
        {
            if (ImUtf8.Button("Export TSV"u8))
            {
                ImGui.OpenPopup(RecordTable.FileNamePopup);
                WriteTsv = true;
            }

            ImUtf8.HoverTooltip("Given a path, export all records as a single TSV file."u8);
        }
        catch
        {
            // ignored
        }

        ImGui.SameLine();
        try
        {
            if (ImUtf8.Button("Copy Caught Fish JSON"u8))
            {
                var logFish = GatherBuddy.GameData.Fishes.Values.Where(f => f.InLog && f.FishingSpots.Count > 0).ToArray();
                var ids     = logFish.Where(f => GatherBuddy.FishLog.IsUnlocked(f)).Select(f => f.ItemId).ToArray();
                Communicator.PrintClipboardMessage("List of ", $"{ids.Length}/{logFish.Length} caught fish ");
                ImGui.SetClipboardText(JsonConvert.SerializeObject(ids, Formatting.Indented));
            }
        }
        catch
        {
            // ignored
        }

        var name = string.Empty;
        if (!ImGuiUtil.OpenNameField(RecordTable.FileNamePopup, ref name) || name.Length <= 0)
            return;

        if (WriteJson)
        {
            try
            {
                var file = new FileInfo(name);
                _plugin.FishRecorder.ExportJson(file);
            }
            catch
            {
                // ignored
            }

            WriteJson = false;
        }

        if (WriteTsv)
        {
            try
            {
                var data = _recordTable.CreateTsv();
                File.WriteAllText(name, data);
                GatherBuddy.Log.Information($"Exported {_recordTable.TotalItems} fish records to {name}.");
            }
            catch (Exception e)
            {
                GatherBuddy.Log.Warning($"Could not export tsv file to {name}:\n{e}");
            }

            WriteTsv = false;
        }
    }
}
