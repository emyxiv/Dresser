using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

using Dresser.Logic;

namespace Dresser.Gui.Components {
	internal static class MiniWindow {
        private static readonly Dictionary<string, MiniWindowSettings> ActiveWindows = [];

        public static void Create(string label, Func<bool?> contents, ImGuiWindowFlags? flags = null, WindowSizeConstraints? sizeConstraints = null) {
            if(ActiveWindows.ContainsKey(label))
                Close(label);

            ActiveWindows.Add(label, new MiniWindowSettings(
                contents,
                flags,
                sizeConstraints
                ));

            PluginLog.Debug($"window created: {label}");

        }

        private static void Close(string label) {
            ActiveWindows.Remove(label);
        }

        public static void Draw() {
            if(ActiveWindows.Count == 0)
                return;

            foreach((var label, var settings) in ActiveWindows) {
                bool? closeOnNotNull = null;

                // display the window and contents
                if(settings.SizeConstraints != null) {
                    ImGui.SetNextWindowSizeConstraints(
                        settings.SizeConstraints.Value.MinimumSize,
                        settings.SizeConstraints.Value.MaximumSize
                        );
                    
                }
                if (ImGui.Begin(label, settings.Flags)) {
                    closeOnNotNull = settings.Contents.Invoke();

                    // handle escape key
                    if(ImGui.IsKeyPressed(ImGuiKey.Escape))
                        closeOnNotNull = true;
                }
                ImGui.End();

                // close if it's closing
                if(closeOnNotNull != null) {
                    Close(label);
                }
            }
        }
    }
    internal struct MiniWindowSettings {
        public Func<bool?> Contents;
        public ImGuiWindowFlags Flags;
        public WindowSizeConstraints? SizeConstraints; // only MinimumSize and MaximumSize supported for now

        public MiniWindowSettings(Func<bool?> contents, ImGuiWindowFlags? flags = null, WindowSizeConstraints? sizeConstraints = null) {
            Contents = contents;
            Flags = flags ?? DefaultFlags();
            SizeConstraints = sizeConstraints ?? DefaultSizeConstraints();
        }

        private static ImGuiWindowFlags DefaultFlags() {
            return
                ImGuiWindowFlags.None
                | ImGuiWindowFlags.AlwaysAutoResize
                | ImGuiWindowFlags.NoDecoration
            ;
        }
        private static WindowSizeConstraints? DefaultSizeConstraints() {
            return 			new WindowSizeConstraints() {
				MinimumSize = new Vector2(20),
				MaximumSize = new Vector2(-1),
			};
        }
    }
}
