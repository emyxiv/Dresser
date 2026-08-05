using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Extensions;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Dresser.UI.Ktk.Nodes.CurrentGearNodes;

public unsafe class PreviewNode : ResNode {

    // configs
    public static Vector2 ViewPortSize => new Vector2(192.0f, 320.0f) * ViewPortScale;
    public static float ViewPortScale => 1.3f;
    public static float ZoomSpeed => 1.8f;
    public static float RotaSpeed => 0.25f;
    public static float MoveSpeed => 0.5f;

    // 
    private readonly AtkUnitBase* _parentAddon;
    private readonly RenderTargetManager* _renderTargetManager;
    private readonly AgentInspect* _agentInspect;
    private readonly InputData* _inputData;
    private readonly CursorInputData* _cursorInputData;
    private readonly Cursor* _cursor;
    private readonly InputManager* _inputManager;
    private readonly Character* _character;
    private readonly ViewportEventListener _previewMouseEventListener;
    public readonly AgentInspect.InspectCharaView* InspectCharaView;

    // nodes
	private readonly ImageNode InspectImage;
    public readonly CollisionNode CollisionNode;
    private readonly NineGridNode Border;


    // data
    private uint _counter;
    private bool _isRota = false;
    private bool _isMove = false;
    private bool _isRotaOrMove = false;
    private Vector2? _lastMousePos = null;
    private Vector2? _backedUpMousePos = null;

    public PreviewNode(AtkUnitBase* parentAddon) {
        _parentAddon = parentAddon;

        var character = PluginServices.Context.LocalPlayer;
        if(character == null) return;
        this._character = (Character*)character.Address;

        this._renderTargetManager = RenderTargetManager.Instance();
        this._agentInspect = AgentInspect.Instance();
        this.InspectCharaView = &this._agentInspect->CharaView;
        this._inputData = &UIInputData.Instance()->InputData;
        this._cursorInputData = &this._inputData->CursorInputs;
        this._cursor = Cursor.Instance();
        this._inputManager = InputManager.Instance();

        // PluginLog.Debug($"camera type: {InspectCharaView->CameraType}");

        Size = ViewPortSize + new Vector2(8f);

        // setup nodes
		this.InspectImage = new ImageNode() {
            NodeId = 8,
			Size = ViewPortSize,
			Position = new Vector2(4, 3),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
        this.CollisionNode = new CollisionNode() {
            NodeId = 16,
            Size = Size,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.EmitsEvents | NodeFlags.Focusable,
        };
		this.Border = new NineGridNode() {
            NodeId = 24,
			Size = Size,
			TopOffset = 14.0f,
			LeftOffset = 14.0f,
			RightOffset = 14.0f,
			BottomOffset = 14.0f,
		};
		this.Border.AddPart(new Part {
			TexturePath = "ui/uld/PreviewA_hr1.tex",
			Size = new Vector2(36.0f, 36.0f),
			TextureCoordinates = new Vector2(0, 0f),
			Id = 0
		});
		var part = this.InspectImage.AddPart(new Part {
            Size = ViewPortSize,
		});
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[1]);
		this._renderTargetManager->CharaViewTextures[1].Value->IncRef();

        // setup inspect viewport
        PluginServices.Framework.RunOnFrameworkThread(() => {
			InspectCharaView->Initialize(&this._agentInspect->AgentInterface, 1, 0);
			InspectCharaView->ModelData.CopyFromCharacter(_character);
		});
        PluginServices.Framework.Update += this.OnFramework;

        // update on first tick
        this.UpdateAppearance();

        // attach nodes
		this.InspectImage.AttachNode(this);
        this.CollisionNode.AttachNode(this);
        this.Border.AttachNode(this);

        // mouse events
        CollisionNode.AddEvent(AtkEventType.MouseDown, MouseDown);
        CollisionNode.AddEvent(AtkEventType.MouseWheel, MouseWheel);
        // mouse move and mouse release can happen outside of the node, so we look at the full viewport
        // TODO: lock mouse would make it unneeded
        _previewMouseEventListener = new ViewportEventListener(ViewPortEvents);
        _previewMouseEventListener.AddEvent(AtkEventType.MouseMove, CollisionNode);
        _previewMouseEventListener.AddEvent(AtkEventType.MouseUp, CollisionNode);
        
    }
	private void OnFramework(IFramework framework) {
		InspectCharaView->Update(this._counter, this._character);
		InspectCharaView->Render(this._counter++);
	}
    protected override void Dispose(bool isNativeDestructor) {
        if (IsDisposed) return;

        PluginServices.Framework.Update -= this.OnFramework;
		InspectCharaView->Release();
        _previewMouseEventListener.RemoveEvent(AtkEventType.MouseMove);
        _previewMouseEventListener.Dispose();

        base.Dispose(isNativeDestructor);
    }


    /////////////////////////
    /// Update appearance ///
    /////////////////////////
    private void UpdateAppearance() {
        InspectCharaView->Update(this._counter, this._character);
        InspectCharaView->ToggleDrawWeapon(true);
        // todo inject dresser data
    }

    /////////////////////////////////////////
    /// Mouse events for pan / yaw / zoom ///
    /////////////////////////////////////////
    private void ViewPortEvents(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        switch (eventType) {
            case AtkEventType.MouseMove: MouseMove(thisPtr, eventType, eventParam, atkEvent, atkEventData); break;
            case AtkEventType.MouseUp:     MouseUp(thisPtr, eventType, eventParam, atkEvent, atkEventData); break;
        }
    }

    private void MouseDown(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        // PluginLog.Debug($"[MOUSE DOWN]");
        // PluginLog.Debug($"   mb flags:({_cursorInputData->MouseButtonHeldFlags},{_cursorInputData->MouseButtonHeldThrottledFlags},{_cursorInputData->MouseButtonPressedFlags},{_cursorInputData->MouseButtonReleasedFlags})");

        var isRotaHeld = _cursorInputData->MouseButtonHeldFlags == MouseButtonFlags.LBUTTON;
        var isMoveHeld = _cursorInputData->MouseButtonHeldFlags == MouseButtonFlags.RBUTTON;
        if (isRotaHeld || isMoveHeld) {
            _isRotaOrMove = true;
            if (isRotaHeld) {
                _isRota = true;
            }
            if (isMoveHeld) {
                _isMove = true;
            }
            _lastMousePos = new Vector2(atkEventData->MouseData.PosX, atkEventData->MouseData.PosY);
            _cursor->IsCursorVisible = false;
            // TODO: mouse position lock
        }
    }

    private void MouseUp(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (!_isRotaOrMove) return;

        // PluginLog.Debug($"   mb flags:({_cursorInputData->MouseButtonHeldFlags},{_cursorInputData->MouseButtonHeldThrottledFlags},{_cursorInputData->MouseButtonPressedFlags},{_cursorInputData->MouseButtonReleasedFlags})");

        var isRotaHeld = _cursorInputData->MouseButtonHeldFlags == MouseButtonFlags.LBUTTON;
        var isMoveHeld = _cursorInputData->MouseButtonHeldFlags == MouseButtonFlags.RBUTTON;
        if(!isRotaHeld) {
            _isRota = false;
        }
        if(!isMoveHeld) {
            _isMove = false;
        }
        if(!isRotaHeld && !isMoveHeld) {
            _isRotaOrMove = false;

            // TODO: release mouse position lock
            _cursor->IsCursorVisible = true;
            _lastMousePos = null;
        }
    }
    private void MouseWheel(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        // TODO: find mouse position lock system because it doesn't work when the cursor is outside

        var wheelDirection = atkEventData->MouseData.WheelDirection;
        var distance = -wheelDirection * ZoomSpeed;

        InspectCharaView->SetCameraDistance(distance);   
    }
    private void MouseMove(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (!_isRotaOrMove) return;
        if(_lastMousePos == null) return;

        // PluginLog.Debug($"[MOUSE MOVE]");
        var mousePosition = new Vector2(atkEventData->MouseData.PosX, atkEventData->MouseData.PosY);
        var mouseDelta = mousePosition - _lastMousePos.Value;

        var mouseYawPitch = mouseDelta * RotaSpeed;
        var mouseXY = new Vector2(mouseDelta.X, -mouseDelta.Y) * (1/_parentAddon->Scale) * MoveSpeed;

        if (_isRota) {
            InspectCharaView->SetCameraYawAndPitch(mouseDelta.X, mouseDelta.Y);

            // even if it's just rota, it's handy to pan Y axis as Pitch rota doesn't work anyway
            InspectCharaView->SetCameraXAndY(0f, mouseXY.Y);
        }
        if (_isMove) {
            InspectCharaView->SetCameraXAndY(mouseXY.X, mouseXY.Y);
        }

        // Save last mouse positions to calculate the delta
        _lastMousePos = mousePosition;
        
        

        if (_backedUpMousePos.HasValue) {
            // PluginLog.Debug($"   reseting");

            // TODO: find out a way to lock camera, or restore backedup positions on each move tick
            
            // _cursorInputData->PositionX = (int)_backedUpMouseCoordinates.Value.X;
            // _cursorInputData->PositionY = (int)_backedUpMouseCoordinates.Value.Y;
            // _cursorInputData->Clear(true, MouseButtonFlags.LBUTTON);
            // _inputData->CursorPositionsChanged = false;
            // _cursor->MouseNotCpatured = true;
            // _cursorInputData->
            // this._inputData
            // this._cursorInputData
            // this._cursor
            // this._inputManager->HeldMouseButtons
            
        }
        // PluginLog.Debug($"   mb flags:({_cursorInputData->MouseButtonHeldFlags},{_cursorInputData->MouseButtonHeldThrottledFlags},{_cursorInputData->MouseButtonPressedFlags},{_cursorInputData->MouseButtonReleasedFlags})");

        // PluginLog.Debug($"   isRotationDragging {isRota} Mouse moved {_lastMouseCoordinates}=>{mousePosition} delta:{mouseDelta}");
        // PluginLog.Debug($"   inputData.state:{atkEventData->InputData.State}  helMB:{this._inputManager->HeldMouseButtons}");
    }
}