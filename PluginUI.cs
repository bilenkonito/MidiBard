﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Game.Internal.Gui.Addon;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using static MidiBard.Plugin;
using Chord = Melanchall.DryWetMidi.MusicTheory.Chord;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;

namespace MidiBard
{
	public class PluginUI
	{
		private readonly string[] uilangStrings = { "EN", "ZH" };
		private static bool Debug = false;
		public bool IsVisible;

		//[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
		//private static void KillTheThread()
		//{
		//	BrowseThread.Abort();
		//}
		//private static Thread BrowseThread;


		private static void HelpMarker(string desc, bool sameline = true)
		{
			if (sameline) ImGui.SameLine();
			//ImGui.PushFont(UiBuilder.IconFont);
			ImGui.TextDisabled("(?)");
			//ImGui.PopFont();
			if (ImGui.IsItemHovered())
			{
				ImGui.PushFont(UiBuilder.DefaultFont);
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
				ImGui.PopFont();
			}
		}

		private static void ToolTip(string desc)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.PushFont(UiBuilder.DefaultFont);
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
				ImGui.PopFont();
			}
		}
		const uint orange = 0xAA00B0E0;
		const uint red = 0xAA0000D0;
		const uint grassgreen1 = 0xFF00A0D0;
		const uint grassgreen2 = 0xFF00A0D0;
		const uint grassgreen3 = 0xFF00A0D0;
		const uint violet = 0xAAFF888E;

		public unsafe void Draw()
		{
			if (!IsVisible)
				return;

			//var Buttoncolor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
			//var ButtonHoveredcolor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
			//var ButtonActivecolor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
			ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
			var scaledWidth = 357 * ImGui.GetIO().FontGlobalScale;
			ImGui.SetNextWindowSizeConstraints(new Vector2(scaledWidth, 0), new Vector2(scaledWidth, 10000));

			//ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign, new Vector2(0.5f, 0.5f));

			//uint color = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
			var ensembleModeRunning = EnsembleModeRunning;
			var ensemblePreparing = MetronomeBeatsElapsed < 0;
			var listeningForEvents = DeviceManager.IsListeningForEvents;

			//if (ensembleModeRunning)
			//{
			//	if (ensemblePreparing)
			//	{
			//		color = orange;
			//	}
			//	else
			//	{
			//		color = red;
			//	}
			//}
			//else
			//{
			//	if (isListeningForEvents)
			//	{
			//		color = violet;
			//	}
			//}
			//ImGui.PushStyleColor(ImGuiCol.TitleBgActive, color);

			try
			{
				//var title = string.Format("MidiBard{0}{1}###midibard",
				//	ensembleModeRunning ? " - Ensemble Running" : string.Empty,
				//	isListeningForEvents ? " - Listening Events" : string.Empty);
				var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5,5));

				if (ImGui.Begin("MidiBard", ref IsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
				{
					void coloredSelectable(uint color, string content)
					{
						ImGui.PushStyleColor(ImGuiCol.Button, color);
						ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
						ImGui.Button(content, new Vector2(-1, ImGui.GetFrameHeight()));
						ImGui.PopStyleColor(2);
					}

					ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
					if (ensembleModeRunning)
					{
						if (ensemblePreparing)
						{
							coloredSelectable(orange, "Ensemble Mode Preparing".Localize());
						}
						else
						{
							coloredSelectable(red, "Ensemble Mode Running".Localize());
						}
					}

					if (listeningForEvents)
					{
						coloredSelectable(violet, "Listening input device: ".Localize() + DeviceManager.CurrentInputDevice.ToDeviceString());
					}

					ImGui.PopStyleVar();

					if (!config.miniPlayer)
					{
						ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
						ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 4));
						{
							if (!_isImportRunning)
							{
								DrawImportButton();
								ToolTip("Import midi file.".Localize());
							}
							else
							{
								DrawImportProgress();
							}

							if (_hasError) DrawFailedImportMessage();

							//ImGui.SameLine();
							//if (ImGui.Button("Remove Selected"))
							//{
							//	PlaylistManager.Remove(PlaylistManager.currentPlaying);
							//}

							ImGui.SameLine();
							if (ImGui.Button("Clear Playlist".Localize())) PlaylistManager.Clear();

							if (localizer.Language == UILang.CN)
							{
								ImGui.PushFont(UiBuilder.IconFont);
								ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(FontAwesomeIcon.QuestionCircle.ToIconString()).X - ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetCursorPosX());
								if (ImGui.Button(FontAwesomeIcon.QuestionCircle.ToIconString()))
								{
									//config.showHelpWindow ^= true;
								}

								ImGui.PopFont();

								if (ImGui.IsItemHovered())
								{
									var currentwindowpos = ImGui.GetWindowPos();
									ImGui.SetNextWindowPos(currentwindowpos + new Vector2(0,
										ImGui.GetTextLineHeightWithSpacing() + ImGui.GetFrameHeightWithSpacing() +
										ImGui.GetStyle().WindowPadding.Y));
									ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
									if (ImGui.Begin("HelpWindow",
										ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.Tooltip |
										ImGuiWindowFlags.AlwaysAutoResize))
									{
										ImGui.BulletText(
											"如何开始使用MIDIBARD演奏？" +
											"\n　MIDIBARD窗口默认在角色进入演奏模式后自动弹出。" +
											"\n　点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表，仅支持.mid格式的乐曲。" +
											"\n　导入时按Ctrl或Shift可以选择多个文件一同导入。" +
											"\n　双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。\n");
										ImGui.BulletText(
											"为什么点击播放之后没有正常演奏？" +
											"\n　MIDIBARD仅使用37键演奏模式。" +
											"\n　请在游戏“乐器演奏操作设置”的“键盘操作”类别下启用“全音阶一同显示、设置按键”的选项。\n");
										ImGui.BulletText(
											"如何使用MIDIBARD进行多人合奏？" +
											"\n　MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。" +
											"\n　合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，请为每位合奏成员分别选择其需要演奏的音轨。" +
											"\n　选择音轨后队长点击节拍器窗口的“合奏准备确认”按钮，" +
											"\n　并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。" +
											"\n　※节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
											"\n　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，在乐曲结束时合奏会自动停止。\n");
										ImGui.BulletText(
											"如何让MIDIBARD为不同乐曲自动切换音调和乐器？" +
											"\n　在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。" +
											"\n　例如：原乐曲文件名为“demo.mid”" +
											"\n　将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。" +
											"\n　将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。" +
											"\n　※可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。");
										ImGui.BulletText(
											"如何为MIDIBARD配置外部Midi输入（如loopMidi或Midi键盘）？" +
											"\n　在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现“正在监听Midi输入”信息后即可使用外部输入。\n");
										ImGui.BulletText(
											"后台演奏时有轻微卡顿不流畅怎么办？" +
											"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 “程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
										ImGui.BulletText("讨论及BUG反馈群：260985966");
										ImGui.Spacing();

										ImGui.End();
									}

									ImGui.PopStyleVar();
								}
							}


						}

						ImGui.PopStyleVar(2);


						if (PlaylistManager.Filelist.Count == 0)
						{
							if (ImGui.Button("Import midi files to start performing!".Localize(), new Vector2(-1, ImGui.GetFrameHeight())))
								RunImportTask();
						}
						else
						{
							ImGui.PushStyleColor(ImGuiCol.Button, 0);
							ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
							ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
							ImGui.PushStyleColor(ImGuiCol.Header, 0x3C60FF8E);
							if (ImGui.BeginTable("##PlaylistTable", 3,
								ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
								ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ContextMenuInBody,
								new Vector2(-1,
									ImGui.GetTextLineHeightWithSpacing() * Math.Min(10,
										PlaylistManager.Filelist.Count)
								)))
							{
								ImGui.TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
								ImGui.TableSetupColumn("##delete", ImGuiTableColumnFlags.WidthFixed);
								ImGui.TableSetupColumn("filename", ImGuiTableColumnFlags.WidthStretch);
								for (var i = 0; i < PlaylistManager.Filelist.Count; i++)
								{
									ImGui.TableNextRow();
									ImGui.TableSetColumnIndex(0);
									if (ImGui.Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentPlaying == i,
										ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
										ImGuiSelectableFlags.AllowItemOverlap))
									{
										if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
										{
											PlaylistManager.CurrentPlaying = i;

											try
											{
												var wasplaying = IsPlaying;
												currentPlayback?.Dispose();
												currentPlayback = null;

												currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].GetFilePlayback();
												if (wasplaying) currentPlayback?.Start();
												Task.Run(SwitchInstrument.WaitSwitchInstrument);
											}
											catch (Exception e)
											{
												//
											}
										}
										else
										{
											PlaylistManager.CurrentSelected = i;
										}
									}

									ImGui.TableNextColumn();
									ImGui.PushFont(UiBuilder.IconFont);
									ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
									if (ImGui.Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}", new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight())))
									{
										PlaylistManager.Remove(i);
									}
									ImGui.PopStyleVar();
									ImGui.PopFont();
									ImGui.TableNextColumn();
									try
									{
										var item2 = PlaylistManager.Filelist[i].Item2;
										ImGui.TextUnformatted(item2);
										ToolTip(item2);
									}
									catch (Exception e)
									{
										ImGui.TextUnformatted("deleted");
									}

								}

								ImGui.EndTable();
							}

							ImGui.PopStyleColor(4);
						}

						#region old playlist

						//ImGui.BeginListBox("##PlayList1", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing() * maxItems));
						//{
						//	var i = 0;
						//	foreach (var tuple in PlaylistManager.Filelist)
						//	{
						//		if (PlaylistManager.currentPlaying == i)
						//		{
						//			ImGui.PushStyleColor(ImGuiCol.Text, config.ThemeColor);
						//		}

						//		if (ImGui.Selectable($"{tuple.Item2}##{i}", PlaylistManager.currentSelected[i], ImGuiSelectableFlags.AllowDoubleClick))
						//		{

						//		}
						//		if (PlaylistManager.currentPlaying == i)
						//		{
						//			ImGui.PopStyleColor();
						//		}
						//		i++;
						//	}
						//}
						//ImGui.EndListBox();
						//ImGui.Text(sb.ToString());

						//if (ImGui.ListBox("##PlayList", ref PlaylistManager.currentPlaying, items, itemsCount, maxItems))
						//{
						//	var wasplaying = IsPlaying;
						//	currentPlayback?.Dispose();

						//	try
						//	{
						//		currentPlayback = PlaylistManager.Filelist[PlaylistManager.currentPlaying].Item1.GetPlayback();
						//		if (wasplaying) currentPlayback?.Start();
						//	}
						//	catch (Exception e)
						//	{

						//	}
						//}

						#endregion

						ImGui.Spacing();
					}

					DrawCurrentPlaying();

					ImGui.Spacing();

					DrawProgressBar();

					ImGui.Spacing();

					ImGui.PushFont(UiBuilder.IconFont);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 4));
					{
						DrawButtonPlayPause();
						DrawButtonStop();
						DrawButtonFastForward();
						DrawButtonPlayMode();
						DrawButtonShowPlayerControl();
						DrawButtonShowSettingsPanel();
						DrawButtonMiniPlayer();
					}
					ImGui.PopFont();
					ImGui.PopStyleVar(2);

					if (config.showMusicControlPanel)
					{
						DrawTrackTrunkSelectionWindow();
						ImGui.Separator();
						DrawPanelMusicControl();
					}
					if (config.showSettingsPanel)
					{
						ImGui.Separator();
						DrawPanelGeneralSettings();
					}
					if (Debug) DrawDebugWindow();

					var size = ImGui.GetWindowSize();
					var pos = ImGui.GetWindowPos();
					var vp = ImGui.GetWindowViewport();



					////ImGui.SetNextWindowViewport(vp.ID);
					//ImGui.SetNextWindowPos(pos + new Vector2(size.X + 1, 0));
					////ImGui.SetNextWindowSizeConstraints(Vector2.Zero, size);
					//if (config.showInstrumentSwitchWindow && ImGui.Begin("Instrument".Localize(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
					//{
					//	ImGui.SetNextItemWidth(120);
					//	UIcurrentInstrument = Plugin.CurrentInstrument;
					//	if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument,
					//		InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount,
					//		(int)InstrumentSheet.RowCount))
					//	{
					//		Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
					//	}

					//	//if (ImGui.Button("Quit"))
					//	//{
					//	//	Task.Run(() => SwitchInstrument.SwitchTo(0));
					//	//}
					//	ImGui.End();
					//}

					ImGui.End();
				}
			}
			finally
			{
				ImGui.PopStyleVar();
				//ImGui.PopStyleColor();
			}

		}

		private static unsafe void DrawCurrentPlaying()
		{
			try
			{
				ImGui.TextColored(new Vector4(0.7f, 1f, 0.5f, 0.9f), $"{PlaylistManager.CurrentPlaying + 1:000} {PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2}");
			}
			catch (Exception e)
			{
				var c = PlaylistManager.Filelist.Count;
				ImGui.TextUnformatted(c > 1
					? $"{PlaylistManager.Filelist.Count} " +
					  "tracks in playlist.".Localize()
					: $"{PlaylistManager.Filelist.Count} " +
					  "track in playlist.".Localize());
			}
		}

		private static unsafe void DrawProgressBar()
		{
			ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0x9C60FF8E);
			//ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x800000A0);



			MetricTimeSpan currentTime = new MetricTimeSpan(0);
			MetricTimeSpan duration = new MetricTimeSpan(0);
			float progress = 0;

			if (currentPlayback != null)
			{
				currentTime = currentPlayback.GetCurrentTime<MetricTimeSpan>();
				duration = currentPlayback.GetDuration<MetricTimeSpan>();
				try
				{
					progress = (float)currentTime.Divide(duration);
				}
				catch (Exception e)
				{
					//
				}

				ImGui.PushStyleColor(ImGuiCol.FrameBg, 0xAC104020);
				ImGui.ProgressBar(progress, new Vector2(-1, 3));
				ImGui.PopStyleColor();
			}
			else
			{
				ImGui.ProgressBar(progress, new Vector2(-1, 3));
			}

			ImGui.TextUnformatted($"{currentTime.Hours}:{currentTime.Minutes:00}:{currentTime.Seconds:00}");
			var durationText = $"{duration.Hours}:{duration.Minutes:00}:{duration.Seconds:00}";
			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(durationText).X);
			ImGui.TextUnformatted(durationText);
			try
			{
				var currentInstrument = PlayingGuitar && !config.OverrideGuitarTones ? (uint)(24 + CurrentGroupTone) : CurrentInstrument;

				string currentInstrumentText;
				if (currentInstrument != 0)
				{
					currentInstrumentText = InstrumentSheet.GetRow(currentInstrument).Instrument;
					if (PlayingGuitar && config.OverrideGuitarTones)
					{
						currentInstrumentText = currentInstrumentText.Split(':', '：').First();
					}
				}
				else
				{
					currentInstrumentText = string.Empty;
				}

				ImGui.SameLine((ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(currentInstrumentText).X) / 2);
				ImGui.TextUnformatted(currentInstrumentText);
			}
			catch (Exception e)
			{
				//
			}

			ImGui.PopStyleColor();
		}

		private static unsafe void DrawButtonMiniPlayer()
		{
			//mini player

			ImGui.SameLine();
			if (ImGui.Button(((FontAwesomeIcon)(config.miniPlayer ? 0xF424 : 0xF422)).ToIconString()))
				config.miniPlayer ^= true;

			ToolTip("Toggle mini player".Localize());
		}


		private static unsafe void DrawButtonShowPlayerControl()
		{
			var Textcolor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
			ImGui.SameLine();
			if (config.showMusicControlPanel)
				ImGui.PushStyleColor(ImGuiCol.Text, 0x9C60FF8E);
			else
				ImGui.PushStyleColor(ImGuiCol.Text, Textcolor);

			if (ImGui.Button((FontAwesomeIcon.Music).ToIconString())) config.showMusicControlPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle player control panel".Localize());
		}

		private static unsafe void DrawButtonShowSettingsPanel()
		{
			var Textcolor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
			ImGui.SameLine();
			if (config.showSettingsPanel)
				ImGui.PushStyleColor(ImGuiCol.Text, 0x9C60FF8E);
			else
				ImGui.PushStyleColor(ImGuiCol.Text, Textcolor);

			if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString())) config.showSettingsPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle settings panel".Localize());
		}


		private static unsafe void DrawButtonPlayPause()
		{
			var PlayPauseIcon =
				IsPlaying ? FontAwesomeIcon.Pause.ToIconString() : FontAwesomeIcon.Play.ToIconString();
			if (ImGui.Button(PlayPauseIcon))
			{
				PluginLog.Debug($"PlayPause pressed. wasplaying: {IsPlaying}");
				if (IsPlaying)
				{
					PlaybackManager.Pause();
				}
				else
				{
					PlaybackManager.Play();
				}
			}
		}

		private static unsafe void DrawButtonStop()
		{
			ImGui.SameLine();
			if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString()))
			{
				PlaybackManager.Stop();
			}
		}

		private static unsafe void DrawButtonFastForward()
		{
			ImGui.SameLine();
			if (ImGui.Button(FontAwesomeIcon.FastForward.ToIconString()))
			{
				PlaybackManager.Next();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				PlaybackManager.Last();
			}
		}

		private static unsafe void DrawButtonPlayMode()
		{

			//playmode button

			ImGui.SameLine();
			FontAwesomeIcon icon;
			switch ((PlayMode)config.PlayMode)
			{
				case PlayMode.Single:
					icon = (FontAwesomeIcon)0xf3e5;
					break;
				case PlayMode.ListOrdered:
					icon = (FontAwesomeIcon)0xf884;
					break;
				case PlayMode.ListRepeat:
					icon = (FontAwesomeIcon)0xf021;
					break;
				case PlayMode.SingleRepeat:
					icon = (FontAwesomeIcon)0xf01e;
					break;
				case PlayMode.Random:
					icon = (FontAwesomeIcon)0xf074;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (ImGui.Button(icon.ToIconString()))
			{
				config.PlayMode += 1;
				config.PlayMode %= 5;
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				config.PlayMode += 4;
				config.PlayMode %= 5;
			}

			ToolTip("Playmode: ".Localize() +
					$"{(PlayMode)config.PlayMode}".Localize());
		}

		private static unsafe void DrawTrackTrunkSelectionWindow()
		{
			if (CurrentTracks?.Any() == true)
			{
				ImGui.Separator();
				ImGui.PushStyleColor(ImGuiCol.Separator, 0);
				//ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(-10,-10));
				//ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(-10, -10));
				//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(-10, -10));
				if (ImGui.BeginChild("TrackTrunkSelection",
					new Vector2(ImGui.GetWindowWidth(), Math.Min(CurrentTracks.Count, 6.6f) * ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().ItemSpacing.Y),
					false, ImGuiWindowFlags.NoDecoration))
				{

					ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, ImGui.GetStyle().ItemSpacing.Y));
					ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.6f, 0));
					if (PlayingGuitar && config.OverrideGuitarTones)
					{
						ImGui.Columns(2);
						ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 4 * ImGui.GetCursorPosX() - ImGui.GetFontSize() * 5.5f - 10);
					}
					for (var i = 0; i < CurrentTracks.Count; i++)
					{
						ImGui.SetCursorPosX(0);
						var configEnabledTrack = !config.EnabledTracks[i];
						if (configEnabledTrack)
						{
							ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));
						}

						if (ImGui.Checkbox($"[{i + 1:00}] {CurrentTracks[i].Item2}", ref config.EnabledTracks[i]))
						{
							try
							{
								//var progress = currentPlayback.GetCurrentTime<MidiTimeSpan>();
								//var wasplaying = IsPlaying;

								currentPlayback?.Dispose();
								//if (wasplaying)
								//{

								//}
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when disposing current playback while changing track selection");
							}
							finally
							{
								currentPlayback = null;
							}
						}

						if (configEnabledTrack)
						{
							ImGui.PopStyleColor();
						}

						if (ImGui.IsItemHovered())
						{
							if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
							{
								ImGui.SetClipboardText(CurrentTracks[i].Item2.ToString());
							}
							ImGui.BeginTooltip();
							ImGui.TextUnformatted(CurrentTracks[i].Item2.ToLongString());
							ImGui.EndTooltip();
						}
						//ToolTip(CurrentTracks[i].Item2.ToLongString()
						//	//+ "\n" +
						//	//("Track Selection. MidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance."
						//	//	.Localize())
						//	);

						if (PlayingGuitar && config.OverrideGuitarTones)
						{
							ImGui.NextColumn();
							var width = ImGui.GetWindowContentRegionWidth();
							//var spacing = ImGui.GetStyle().ItemSpacing.X;
							var buttonSize = new Vector2(ImGui.GetFontSize() * 1.1f, ImGui.GetFrameHeight());
							const uint colorRed = 0xee_6666bb;
							const uint colorCyan = 0xee_bbbb66;
							const uint colorGreen = 0xee_66bb66;
							const uint colorYellow = 0xee_66bbbb;
							const uint colorBlue = 0xee_bb6666;
							void drawButton(int toneID, uint color, string toneName, int track)
							{
								//ImGui.SameLine(width - (4.85f - toneID) * 3 * spacing);
								var DrawColor = config.TracksTone[track] == toneID;
								if (DrawColor)
								{
									ImGui.PushStyleColor(ImGuiCol.Button, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
								}

								if (ImGui.Button($"{toneName}##toneSwitchButton{i}", buttonSize))
								{
									config.TracksTone[track] = toneID;
								}

								if (DrawColor)
								{
									ImGui.PopStyleColor(3);
								}
							}


							drawButton(0, colorRed, " I ", i);
							ImGui.SameLine();
							drawButton(1, colorCyan, " II ", i);
							ImGui.SameLine();
							drawButton(2, colorGreen, "III", i);
							ImGui.SameLine();
							drawButton(3, colorYellow, "IV", i);
							ImGui.SameLine();
							drawButton(4, colorBlue, "V", i);
							ImGui.NextColumn();
						}
					}

					ImGui.PopStyleVar(3);
					ImGui.EndChild();
				}
				//ImGui.PopStyleVar(3);
				ImGui.PopStyleColor();
			}
		}


		private static void DrawPanelMusicControl()
		{
			UIcurrentInstrument = Plugin.CurrentInstrument;
			if (ImGui.Combo("Instrument".Localize(), ref UIcurrentInstrument, InstrumentStrings, InstrumentStrings.Length, 20))
			{
				Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument, true));
			}
			ToolTip("Select current instrument. \nRight click to quit performance mode.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				Task.Run(() => SwitchInstrument.SwitchTo(0));
				PlaybackManager.Pause();
			}

			if (currentPlayback != null)
			{
				var currentTime = currentPlayback.GetCurrentTime<MetricTimeSpan>();
				var duration = currentPlayback.GetDuration<MetricTimeSpan>();
				float progress;
				try
				{
					progress = (float)currentTime.Divide(duration);
				}
				catch (Exception e)
				{
					progress = 0;
				}

				if (ImGui.SliderFloat("Progress".Localize(), ref progress, 0, 1,
					$"{currentTime.Minutes}:{currentTime.Seconds:00}",
					ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat))
				{
					currentPlayback.MoveToTime(duration.Multiply(progress));
				}

				if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				{
					currentPlayback.MoveToTime(duration.Multiply(0));
				}
			}
			else
			{
				float zeroprogress = 0;
				ImGui.SliderFloat("Progress".Localize(), ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
			}
			ToolTip("Set the playing progress. \nRight click to restart current playback.".Localize());

			#region bpm

			Tempo bpm = null;
			try
			{
				// ReSharper disable once PossibleNullReferenceException
				var current = currentPlayback.GetCurrentTime(TimeSpanType.Midi);
				bpm = currentPlayback.TempoMap.GetTempoAtTime(current);
			}
			catch
			{
				//
			}

			var label = $"{config.playSpeed:F2}";

			if (bpm != null) label += $" ({bpm.BeatsPerMinute * config.playSpeed:F1} bpm)";

			#endregion

			if (ImGui.DragFloat("Speed".Localize(), ref config.playSpeed, 0.003f, 0.1f, 10f, label, ImGuiSliderFlags.Logarithmic))
			{
				SetSpeed();
			}
			ToolTip("Set the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				config.playSpeed = 1;
				SetSpeed();
			}

			void SetSpeed()
			{
				try
				{
					config.playSpeed = Math.Max(0.1f, config.playSpeed);
					var currenttime = currentPlayback.GetCurrentTime(TimeSpanType.Midi);
					currentPlayback.Speed = config.playSpeed;
					currentPlayback.MoveToTime(currenttime);
				}
				catch (Exception e)
				{
				}
			}


			ImGui.InputInt("Transpose".Localize(), ref config.NoteNumberOffset, 12);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) config.NoteNumberOffset = 0;
			ToolTip("Pitch shift, Measured by semitone. \nRight click to reset.".Localize());


			if (ImGui.Button("Octave+".Localize())) config.NoteNumberOffset += 12;
			ToolTip("Add 1 octave(+12 semitones) to all notes.".Localize());

			ImGui.SameLine();
			if (ImGui.Button("Octave-".Localize())) config.NoteNumberOffset -= 12;
			ToolTip("Subtract 1 octave(-12 semitones) to all notes.".Localize());

			ImGui.SameLine();
			if (ImGui.Button("Reset##note".Localize())) config.NoteNumberOffset = 0;

			ImGui.SameLine();
			ImGui.Checkbox("Auto Adapt".Localize(), ref config.AdaptNotesOOR);
			HelpMarker("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play".Localize());

			//ImGui.SliderFloat("secbetweensongs", ref config.timeBetweenSongs, 0, 10,
			//	$"{config.timeBetweenSongs:F2} [{500000 * config.timeBetweenSongs:F0}]", ImGuiSliderFlags.AlwaysClamp);
		}

		private void DrawPanelGeneralSettings()
		{
			//ImGui.SliderInt("Playlist size".Localize(), ref config.playlistSizeY, 2, 50,
			//	config.playlistSizeY.ToString(), ImGuiSliderFlags.AlwaysClamp);
			//ToolTip("Play list rows number.".Localize());

			//ImGui.SliderInt("Player width".Localize(), ref config.playlistSizeX, 356, 1000, config.playlistSizeX.ToString(), ImGuiSliderFlags.AlwaysClamp);
			//ToolTip("Player window max width.".Localize());

			//var inputDevices = InputDevice.GetAll().ToList();
			//var currentDeviceInt = inputDevices.FindIndex(device => device == CurrentInputDevice);

			//if (ImGui.Combo(CurrentInputDevice.ToString(), ref currentDeviceInt, inputDevices.Select(i => $"{i.Id} {i.Name}").ToArray(), inputDevices.Count))
			//{
			//	//CurrentInputDevice.Connect(CurrentOutputDevice);
			//}

			var inputDevices = DeviceManager.Devices;
			//ImGui.BeginListBox("##auofhiao", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing()* (inputDevices.Length + 1)));
			if (ImGui.BeginCombo("Input Device".Localize(), DeviceManager.CurrentInputDevice.ToDeviceString()))
			{
				if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
				{
					DeviceManager.DisposeDevice();
				}
				for (int i = 0; i < inputDevices.Length; i++)
				{
					var device = inputDevices[i];
					if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
					{
						DeviceManager.SetDevice(device);
					}
				}
				ImGui.EndCombo();
			}

			if (ImGui.Combo("UI Language".Localize(), ref config.uiLang, uilangStrings, 2))
			{
				localizer = new Localizer((UILang)config.uiLang);
			}


			ImGui.Checkbox("Auto open MidiBard".Localize(), ref config.AutoOpenPlayerWhenPerforming);
			HelpMarker("Open MidiBard window automatically when entering performance mode".Localize());
			//ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
			//if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
			ImGui.Checkbox("Monitor ensemble".Localize(), ref config.MonitorOnEnsemble);
			HelpMarker("Auto start ensemble when entering in-game party ensemble mode.".Localize());


			ImGui.Checkbox("Auto pitch shift".Localize(), ref config.autoPitchShift);
			HelpMarker("Auto pitch shift notes on demand. If you need this, \nplease add #pitch shift number# before file name.\nE.g. #-12#demo.mid".Localize());
			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Auto switch instrument".Localize(), ref config.autoSwitchInstrument);
			HelpMarker("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid".Localize());
			ImGui.Checkbox("Override Guitar Tones".Localize(), ref config.OverrideGuitarTones);
			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
			if (ImGui.Button("Debug info", new Vector2(-1, ImGui.GetFrameHeight()))) Debug = !Debug;
			//if (ImGui.ColorPicker4("Theme Color", ref config.ThemeColor, ImGuiColorEditFlags.AlphaBar))
			//{

			//}
		}
		private static void DrawDebugWindow()
		{
			//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 0));
			if (ImGui.Begin("MIDIBARD DEBUG", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
			{
				try
				{
					ImGui.TextUnformatted($"AgentModule: {AgentManager.AgentModule.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentModule")) ImGui.SetClipboardText($"{AgentManager.AgentModule.ToInt64():X}");

					ImGui.TextUnformatted($"UiModule: {AgentManager.UiModule.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##4")) ImGui.SetClipboardText($"{AgentManager.UiModule.ToInt64():X}");
					ImGui.TextUnformatted($"AgentCount:{AgentManager.Agents.Count}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				try
				{
					ImGui.TextUnformatted($"AgentPerformance: {PerformanceAgent.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentPerformance")) ImGui.SetClipboardText($"{PerformanceAgent.Pointer.ToInt64():X}");

					ImGui.TextUnformatted($"AgentID: {PerformanceAgent.Id}");

					ImGui.TextUnformatted($"notePressed: {notePressed}");
					ImGui.TextUnformatted($"noteNumber: {noteNumber}");
					ImGui.TextUnformatted($"InPerformanceMode: {InPerformanceMode}");
					ImGui.TextUnformatted($"Timer1: {TimeSpan.FromMilliseconds(PerformanceTimer1)}");
					ImGui.TextUnformatted($"Timer2: {TimeSpan.FromTicks(PerformanceTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();

				try
				{
					ImGui.TextUnformatted($"AgentMetronome: {MetronomeAgent.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentMetronome")) ImGui.SetClipboardText($"{MetronomeAgent.Pointer.ToInt64():X}");
					ImGui.TextUnformatted($"AgentID: {MetronomeAgent.Id}");


					ImGui.TextUnformatted($"Running: {MetronomeRunning}");
					ImGui.TextUnformatted($"Ensemble: {EnsembleModeRunning}");
					ImGui.TextUnformatted($"BeatsElapsed: {MetronomeBeatsElapsed}");
					ImGui.TextUnformatted($"PPQN: {MetronomePPQN} ({60_000_000 / (double)MetronomePPQN:F3}bpm)");
					ImGui.TextUnformatted($"BeatsPerBar: {MetronomeBeatsperBar}");
					ImGui.TextUnformatted($"Timer1: {TimeSpan.FromMilliseconds(MetronomeTimer1)}");
					ImGui.TextUnformatted($"Timer2: {TimeSpan.FromTicks(MetronomeTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}



				ImGui.Separator();
				try
				{
					ImGui.TextUnformatted($"PerformInfos: {PerformInfos.ToInt64() + 3:X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##PerformInfos")) ImGui.SetClipboardText($"{PerformInfos.ToInt64() + 3:X}");
					ImGui.TextUnformatted($"CurrentInstrumentKey: {CurrentInstrument}");
					ImGui.TextUnformatted($"Instrument: {InstrumentSheet.GetRow(CurrentInstrument).Instrument}");
					ImGui.TextUnformatted($"Name: {InstrumentSheet.GetRow(CurrentInstrument).Name.RawString}");
					ImGui.TextUnformatted($"Tone: {CurrentGroupTone}");
					//ImGui.Text($"unkFloat: {UnkFloat}");
					////ImGui.Text($"unkByte: {UnkByte1}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				ImGui.TextUnformatted($"currentPlaying: {PlaylistManager.CurrentPlaying}");
				ImGui.TextUnformatted($"currentSelected: {PlaylistManager.CurrentSelected}");
				ImGui.TextUnformatted($"FilelistCount: {PlaylistManager.Filelist.Count}");
				ImGui.TextUnformatted($"currentUILanguage: {pluginInterface.UiLanguage}");

				ImGui.Separator();
				try
				{
					//var devicesList = DeviceManager.Devices.Select(i => i.ToDeviceString()).ToArray();


					//var inputDevices = DeviceManager.Devices;
					////ImGui.BeginListBox("##auofhiao", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing()* (inputDevices.Length + 1)));
					//if (ImGui.BeginCombo("Input Device", DeviceManager.CurrentInputDevice.ToDeviceString()))
					//{
					//	if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
					//	{
					//		DeviceManager.DisposeDevice();
					//	}
					//	for (int i = 0; i < inputDevices.Length; i++)
					//	{
					//		var device = inputDevices[i];
					//		if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
					//		{
					//			DeviceManager.SetDevice(device);
					//		}
					//	}
					//	ImGui.EndCombo();
					//}



					//ImGui.EndListBox();

					//if (ImGui.ListBox("##????", ref InputDeviceID, devicesList, devicesList.Length))
					//{
					//	if (InputDeviceID == 0)
					//	{
					//		DeviceManager.DisposeDevice();
					//	}
					//	else
					//	{
					//		DeviceManager.SetDevice(InputDevice.GetByName(devicesList[InputDeviceID]));
					//	}
					//}

					if (ImGui.SmallButton("Start Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StartEventsListening();
					}
					ImGui.SameLine();
					if (ImGui.SmallButton("Stop Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StopEventsListening();
					}

					ImGui.TextUnformatted($"InputDevices: {InputDevice.GetDevicesCount()}\n{string.Join("\n", InputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}"))}");
					ImGui.TextUnformatted($"OutputDevices: {OutputDevice.GetDevicesCount()}\n{string.Join("\n", OutputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}({i.DeviceType})"))}");

					ImGui.TextUnformatted($"CurrentInputDevice: \n{DeviceManager.CurrentInputDevice} Listening: {DeviceManager.CurrentInputDevice?.IsListeningForEvents}");
					ImGui.TextUnformatted($"CurrentOutputDevice: \n{CurrentOutputDevice}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}

				#region Generator

				//ImGui.Separator();

				//if (ImGui.BeginChild("Generate", new Vector2(size - 5, 150), false, ImGuiWindowFlags.NoDecoration))
				//{
				//	ImGui.DragInt("length##keyboard", ref config.testLength, 0.05f);
				//	ImGui.DragInt("interval##keyboard", ref config.testInterval, 0.05f);
				//	ImGui.DragInt("repeat##keyboard", ref config.testRepeat, 0.05f);
				//	if (config.testLength < 0)
				//	{
				//		config.testLength = 0;
				//	}

				//	if (config.testInterval < 0)
				//	{
				//		config.testInterval = 0;
				//	}

				//	if (config.testRepeat < 0)
				//	{
				//		config.testRepeat = 0;
				//	}

				//	if (ImGui.Button("generate##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();

				//		}
				//		catch (Exception e)
				//		{
				//			//
				//		}

				//		static Pattern GetSequence(int octave)
				//		{
				//			return new PatternBuilder()
				//				.SetRootNote(Note.Get(NoteName.C, octave))
				//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//				.Note(Interval.Zero)
				//				.StepForward()
				//				.Note(Interval.One)
				//				.StepForward()
				//				.Note(Interval.Two)
				//				.StepForward()
				//				.Note(Interval.Three)
				//				.StepForward()
				//				.Note(Interval.Four)
				//				.StepForward()
				//				.Note(Interval.Five)
				//				.StepForward()
				//				.Note(Interval.Six)
				//				.StepForward()
				//				.Note(Interval.Seven)
				//				.StepForward()
				//				.Note(Interval.Eight)
				//				.StepForward()
				//				.Note(Interval.Nine)
				//				.StepForward()
				//				.Note(Interval.Ten)
				//				.StepForward()
				//				.Note(Interval.Eleven)
				//				.StepForward().Build();
				//		}

				//		static Pattern GetSequenceDown(int octave)
				//		{
				//			return new PatternBuilder()
				//				.SetRootNote(Note.Get(NoteName.C, octave))
				//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//				.Note(Interval.Eleven)
				//				.StepForward()
				//				.Note(Interval.Ten)
				//				.StepForward()
				//				.Note(Interval.Nine)
				//				.StepForward()
				//				.Note(Interval.Eight)
				//				.StepForward()
				//				.Note(Interval.Seven)
				//				.StepForward()
				//				.Note(Interval.Six)
				//				.StepForward()
				//				.Note(Interval.Five)
				//				.StepForward()
				//				.Note(Interval.Four)
				//				.StepForward()
				//				.Note(Interval.Three)
				//				.StepForward()
				//				.Note(Interval.Two)
				//				.StepForward()
				//				.Note(Interval.One)
				//				.StepForward()
				//				.Note(Interval.Zero)
				//				.StepForward()
				//				.Build();
				//		}

				//		Pattern pattern = new PatternBuilder()

				//			.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))

				//			.Pattern(GetSequence(3))
				//			.Pattern(GetSequence(4))
				//			.Pattern(GetSequence(5))
				//			.SetRootNote(Note.Get(NoteName.C, 5))
				//			.StepForward()
				//			.Note(Interval.Twelve)
				//			.Pattern(GetSequenceDown(5))
				//			.Pattern(GetSequenceDown(4))
				//			.Pattern(GetSequenceDown(3))
				//			// Get pattern
				//			.Build();

				//		var repeat = new PatternBuilder().Pattern(pattern).Repeat(config.testRepeat).Build();

				//		testplayback = repeat.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
				//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
				//	}

				//	ImGui.SameLine();
				//	if (ImGui.Button("chord##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();

				//		}
				//		catch (Exception e)
				//		{
				//			//
				//		}

				//		var pattern = new PatternBuilder()
				//			//.SetRootNote(Note.Get(NoteName.C, 3))
				//			//C-G-Am-(G,Em,C/G)-F-(C,Em)-(F,Dm)-G
				//			.SetOctave(Octave.Get(3))
				//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.A, ChordQuality.Minor)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)

				//			.Build();

				//		testplayback = pattern.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
				//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
				//	}

				//	ImGui.Spacing();
				//	if (ImGui.Button("play##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.MoveToStart();
				//			testplayback?.Start();
				//		}
				//		catch (Exception e)
				//		{
				//			PluginLog.Error(e.ToString());
				//		}
				//	}

				//	ImGui.SameLine();
				//	if (ImGui.Button("dispose##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();
				//		}
				//		catch (Exception e)
				//		{
				//			PluginLog.Error(e.ToString());
				//		}
				//	}

				//	try
				//	{
				//		ImGui.TextUnformatted($"{testplayback.GetDuration(TimeSpanType.Metric)}");
				//	}
				//	catch (Exception e)
				//	{
				//		ImGui.TextUnformatted("null");
				//	}
				//	//ImGui.SetNextItemWidth(120);
				//	//UIcurrentInstrument = Plugin.CurrentInstrument;
				//	//if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument, InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount, (int)InstrumentSheet.RowCount))
				//	//{
				//	//	Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
				//	//}

				//	//if (ImGui.Button("Quit"))
				//	//{
				//	//	Task.Run(() => SwitchInstrument.SwitchTo(0));
				//	//}

				//	ImGui.EndChild();
				//}

				#endregion

				ImGui.End();
			}
			//ImGui.PopStyleVar();

		}

		private static int UIcurrentInstrument;
		#region import

		private const string LabelTab = "Import Mods";
		private const string LabelImportButton = "Import TexTools Modpacks";
		private const string LabelFileDialog = "Pick one or more modpacks.";
		private static readonly string LabelFileImportRunning = "Import in progress...".Localize();
		private const string FileTypeFilter = "TexTools TTMP Modpack (*.ttmp2)|*.ttmp*|All files (*.*)|*.*";
		private const string TooltipModpack1 = "Writing modpack to disk before extracting...";

		private static readonly string FailedImport =
			"One or more of your modpacks failed to import.\nPlease submit a bug report.".Localize();

		private const uint ColorRed = 0xFF0000C8;

		private void DrawImportButton()
		{
			ImGui.PushFont(UiBuilder.IconFont);
			if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
			{
				ImGui.PopFont();
				RunImportTask();
			}
			else
			{
				ImGui.PopFont();
			}
		}

		private static void DrawFailedImportMessage()
		{
			ImGui.PushStyleColor(ImGuiCol.Text, ColorRed);
			ImGui.TextUnformatted(FailedImport);
			ImGui.PopStyleColor();
		}

		private void DrawImportProgress()
		{
			ImGui.Button(LabelFileImportRunning);

			//if (_texToolsImport == null)
			//{
			//	return;
			//}

			//switch (_texToolsImport.State)
			//{
			//	case ImporterState.None:
			//		break;
			//	case ImporterState.WritingPackToDisk:
			//		ImGui.Text(TooltipModpack1);
			//		break;
			//	case ImporterState.ExtractingModFiles:
			//	{
			//		var str =
			//			$"{_texToolsImport.CurrentModPack} - {_texToolsImport.CurrentProgress} of {_texToolsImport.TotalProgress} files";

			//		ImGui.ProgressBar(_texToolsImport.Progress, ImportBarSize, str);
			//		break;
			//	}
			//	case ImporterState.Done:
			//		break;
			//	default:
			//		throw new ArgumentOutOfRangeException();
			//}
		}

		private bool _isImportRunning;
		private bool _hasError;

		public bool IsImporting()
		{
			return _isImportRunning;
		}



		private void RunImportTask()
		{
			if (!_isImportRunning)
			{
				_isImportRunning = true;
				var b = new Browse((result, filePath) =>
				{
					if (result == DialogResult.OK)
					{
						PlaylistManager.ImportMidiFile(filePath, true);
					}

					_isImportRunning = false;
				});

				var t = new Thread(b.BrowseDLL);
				t.SetApartmentState(ApartmentState.STA);
				t.Start();
			}

			//_isImportRunning = true;
			//Task.Run(async () =>
			//{
			//	var picker = new OpenFileDialog
			//	{
			//		Multiselect = true,
			//		Filter = "midi file (*.mid)|*.mid",
			//		CheckFileExists = true,
			//		Title = "Select a mid file"
			//	};

			//	var result = await picker.ShowDialogAsync();

			//	if (result == DialogResult.OK)
			//	{
			//		_hasError = false;

			//		PlaylistManager.ImportMidiFile(picker.FileNames, true);

			//		//_texToolsImport = null;
			//		//_base.ReloadMods();
			//	}

			//	_isImportRunning = false;
			//});


		}



		#endregion
	}
}