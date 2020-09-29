namespace Ganymate.Visualization

open System.Diagnostics
open System.Numerics
open ImGuiNET
open Veldrid
open Veldrid.StartupUtilities

module Program =
    let createWindow =
        let windowCreateInfo =
            WindowCreateInfo(
                X = 400,
                Y = 400,
                WindowHeight = 640,
                WindowWidth = 480,
                WindowTitle = "Ganymate.Visualization"            
            )
        let window = VeldridStartup.CreateWindow windowCreateInfo
        window.add_Closed (fun () -> exit 0 |> ignore )
        window.Resizable <- true
        window.BorderVisible <-true
        window

    type Infra =
        {
            Window: Sdl2.Sdl2Window
            Gui: ImGuiRenderer
            CommandList: CommandList
            GraphicsDevice: GraphicsDevice
            StopWatch: Stopwatch
        }

    type State =
        {
            isClicked : Option<int>
            lastFrame : int64
        }

    let drawFrame (infrastructure:Infra) isClicked elapsedTime =
        let events = infrastructure.Window.PumpEvents()
        let frameRate = 1000000.0f / float32 elapsedTime
        infrastructure.Gui.Update(float32 elapsedTime, events)
        ImGui.SetNextWindowSize(Vector2(float32 (infrastructure.Window.Width), float32 infrastructure.Window.Height * 0.25f))
        ImGui.SetNextWindowPos(Vector2(float32 0, float32 infrastructure.Window.Height * 0.75f), ImGuiCond.Always)

        ImGui.Begin(
            "main",
            ImGuiWindowFlags.NoMove
            ||| ImGuiWindowFlags.NoCollapse
            ||| ImGuiWindowFlags.NoTitleBar
            ||| ImGuiWindowFlags.NoResize)
        |> ignore

        let text = sprintf "FPS %6.2f" frameRate

        ImGui.Text(text)

        ImGui.SetNextWindowPos(Vector2(10.f, 20.f))
        ImGui.Separator()
        ImGui.Indent 25.f

        isClicked
        |> Option.map (fun _ -> "IsClicked")
        |> Option.defaultValue "--"
        |> ImGui.Text

        ImGui.Button("test")
        let isJustClicked = ImGui.IsItemClicked ImGuiMouseButton.Left
        ImGui.End()

        infrastructure.CommandList.Begin()
        infrastructure.CommandList.SetFramebuffer(infrastructure.GraphicsDevice.MainSwapchain.Framebuffer)

        if ImGui.GetIO().WantCaptureMouse
        then infrastructure.CommandList.ClearColorTarget(uint32 0, RgbaFloat.Black)
        else infrastructure.CommandList.ClearColorTarget(uint32 0, RgbaFloat.CornflowerBlue)

        infrastructure.Gui.Render (infrastructure.GraphicsDevice, infrastructure.CommandList)

        infrastructure.CommandList.End()

        infrastructure.GraphicsDevice.SubmitCommands infrastructure.CommandList
        infrastructure.GraphicsDevice.SwapBuffers()
        isJustClicked


    let rec drawFrameOrExit infrastructure state =
        if not infrastructure.Window.Exists
        then 1
        else
            // actual drawing code
            let currentFrame = infrastructure.StopWatch.ElapsedTicks
            let elapsedTime = currentFrame - state.lastFrame

            let isJustClicked = drawFrame infrastructure state.isClicked elapsedTime

            {
                lastFrame = currentFrame
                isClicked =
                    match state.isClicked with
                    | Some 0 -> None
                    | Some x -> Some (x - 1)
                    | None ->
                        if isJustClicked
                        then Some 20_000
                        else None
            }
            |> drawFrameOrExit infrastructure

    [<EntryPoint>]
    let main argv =
        let window = createWindow
        let gdo = GraphicsDeviceOptions()
        let gd = VeldridStartup.CreateGraphicsDevice window
        let gui =
            new ImGuiRenderer(
                gd,
                (gd.MainSwapchain.Framebuffer.OutputDescription),
                window.Width,
                window.Height
            )
        let sw = Stopwatch.StartNew()
            
        window.add_Resized (fun () ->
            gd.ResizeMainWindow(uint32 window.Width, uint32 window.Height))

        let cl = gd.ResourceFactory.CreateCommandList()
        let infra =
            {
                Infra.CommandList = cl
                Infra.GraphicsDevice = gd
                Infra.Gui = gui
                Infra.StopWatch = sw
                Infra.Window = window
            }
        let state =
            {
                State.lastFrame = sw.ElapsedTicks
                State.isClicked = Some 10_000
            }

        drawFrameOrExit infra state