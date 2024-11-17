using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Websocket.Client;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();

var exitEvent = new ManualResetEvent(false);
Console.WriteLine("Enter IP Address of Nova (for example: 192.168.4.10)");
var ipAddress = Console.ReadLine();
var url = new Uri("ws://" + ipAddress + "/ws");

var TRINNOV_OPTIMISED = true;
var TRINNOV_IS_DIMMED = false;
var TRINNOV_IS_MUTED = false;
var TRINNOV_IS_ANALOG = false;
var TRINNOV_VOLUME = 0;

const int CONNECTION_RESET = 0;
const int OPTIMISER_TOGGLE = 2;
const int INPUT_TOGGLE = 3;
const int VOL_MUTE_TOGGLE = 4;
const int DIM_TOGGLE = 6;
const int REFERENCE_LEVEL = 7;
const int VOL_DOWN = 5;
const int VOL_UP = 1;

using var ctx = DeviceContext.Create()
        //   .AddListener<StreamDeckListener>()
        .AddListener<SocketIOBoardListener>()
    ;

using var board = await ctx.OpenAsync();
using var client = new WebsocketClient(url);

client.ReconnectTimeout = null;
client.MessageReceived.Subscribe(msg =>
{
    var message = Encoding.UTF8.GetString(msg.Binary);
    Log.Debug($"Message received: {message}");

    var bypass = Regex.Match(message, @".*\/optimizer\/volume\/bypass\{.*?""[^""]*"":(true|false)\}.*");
    if (bypass.Success)
    {
        TRINNOV_OPTIMISED = bypass.Groups[1].Value != "true";
        ImageKey(OPTIMISER_TOGGLE, TRINNOV_OPTIMISED ? "optimiser-on" : "optimiser-off");
    }

    //0 /optimizer/volume/display_volume{"_":-20.0}$/optimizer/volume/dim{"_":true}
    var display_volume = Regex.Match(message, @".*\/optimizer\/volume\/display_volume\{.*?""_"":(-?\d+(\.\d+)?)\}");
    if (display_volume.Success)
    {
        TRINNOV_VOLUME = Convert.ToInt32(double.Parse(display_volume.Groups[1].Value));
        ImageKey(VOL_MUTE_TOGGLE, TRINNOV_VOLUME == 0 ? "vol-zero" : "vol", true);
    }

    var dim = Regex.Match(message, @".*\/optimizer\/volume\/dim\{.*?""[^""]*"":(true|false)\}.*");
    if (dim.Success)
    {
        TRINNOV_IS_DIMMED = dim.Groups[1].Value == "true";
        ImageKey(DIM_TOGGLE, TRINNOV_IS_DIMMED ? "dim-on" : "dim");
    }

    var mute = Regex.Match(message, @".*\/optimizer\/volume\/mute\{.*?""[^""]*"":(true|false)\}.*");
    if (mute.Success)
    {
        TRINNOV_IS_MUTED = mute.Groups[1].Value == "true";
        ImageKey(VOL_MUTE_TOGGLE, TRINNOV_IS_MUTED ? "vol-mute" : TRINNOV_VOLUME == 0 ? "vol-zero" : "vol", true);
    }
});
client.DisconnectionHappened.Subscribe(msg => Log.Information($"Message received: {msg.Exception}"));
client.ReconnectionHappened.Subscribe(info => Log.Information($"Reconnection happened, type: {info.Type}"));
await client.StartOrFail();

// Set PC (Digital)
Log.Debug("Digital sent");
Task.Run(() => client.Send(new byte[]
{
    0x00, 0x00, 0x00, 0x34, 0x03, 0x00, 0x00, 0x00,
    0x14, 0x2F, 0x6D, 0x6F, 0x6E, 0x69, 0x74, 0x6F,
    0x72, 0x69, 0x6E, 0x67, 0x2F, 0x63, 0x6F, 0x6E,
    0x74, 0x72, 0x6F, 0x6C, 0x2F, 0x7B, 0x22, 0x6D,
    0x61, 0x69, 0x6E, 0x5F, 0x73, 0x65, 0x6C, 0x65,
    0x63, 0x74, 0x65, 0x64, 0x5F, 0x73, 0x6F, 0x75,
    0x72, 0x63, 0x65, 0x73, 0x22, 0x3A, 0x35, 0x7D
}));

// Get the Mute status
Task.Run(() => client.Send(new byte[]
{
    0x00, 0x00, 0x00, 0x1B, 0x01, 0x00, 0x00, 0x00,
    0x16, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
    0x6D, 0x65, 0x2F, 0x6D, 0x75, 0x74, 0x65
}));

// Get the Optimiser bypass status
Task.Run(() => client.Send(new byte[]
{
    0x00, 0x00, 0x00, 0x1D, 0x01, 0x00, 0x00, 0x00,
    0x18, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
    0x6D, 0x65, 0x2F, 0x62, 0x79, 0x70, 0x61, 0x73,
    0x73
}));

// Get the Display Volume
Task.Run(() => client.Send(new byte[]
{
    0x00, 0x00, 0x00, 0x25, 0x01, 0x00, 0x00, 0x00,
    0x20, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
    0x6D, 0x65, 0x2F, 0x64, 0x69, 0x73, 0x70, 0x6C,
    0x61, 0x79, 0x5F, 0x76, 0x6F, 0x6C, 0x75, 0x6D,
    0x65
}));

// Get the Dim status
Task.Run(() => client.Send(new byte[]
{
    0x00, 0x00, 0x00, 0x1A, 0x01, 0x00, 0x00, 0x00,
    0x15, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
    0x6D, 0x65, 0x2F, 0x64, 0x69, 0x6D
}));

ImageKey(CONNECTION_RESET, "reconnect-connected");
ImageKey(OPTIMISER_TOGGLE, "optimiser-on");
ImageKey(DIM_TOGGLE, "dim");
ImageKey(REFERENCE_LEVEL, "ref");
ImageKey(VOL_MUTE_TOGGLE, "vol");
ImageKey(INPUT_TOGGLE, "digital");
ImageKey(VOL_DOWN, "vol-down");
ImageKey(VOL_UP, "vol-up");

board.KeyStateChanged += HandleKeyPress!;

Console.ReadKey();

exitEvent.WaitOne();
return;

void HandleKeyPress(object sender, KeyEventArgs arg)
{
    switch (arg.Key)
    {
        case VOL_DOWN when arg.IsDown:
            ImageKey(VOL_DOWN, "vol-down-press");
            return;
        case VOL_DOWN:
            ImageKey(VOL_DOWN, "vol-down");
            Task.Run(() => client.Send(new byte[]
            {
                0x00, 0x00, 0x00, 0x24, 0x03, 0x00, 0x00, 0x00,
                0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                0x6D, 0x65, 0x7B, 0x22, 0x64, 0x76, 0x6F, 0x6C,
                0x75, 0x6D, 0x65, 0x22, 0x3A, 0x2D, 0x31, 0x7D
            }));
            break;
        case VOL_UP when arg.IsDown:
            ImageKey(VOL_UP, "vol-up-press");
            return;
        case VOL_UP:
            ImageKey(VOL_UP, "vol-up");
            Log.Debug("Volume up sent");
            Task.Run(() => client.Send(new byte[]
            {
                0x00, 0x00, 0x00, 0x23, 0x03, 0x00, 0x00, 0x00,
                0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                0x6D, 0x65, 0x7B, 0x22, 0x64, 0x76, 0x6F, 0x6C,
                0x75, 0x6D, 0x65, 0x22, 0x3A, 0x31, 0x7D
            }));
            break;
        case VOL_MUTE_TOGGLE when arg.IsDown:
            ImageKey(VOL_MUTE_TOGGLE, "vol-press", true);
            return;
        case VOL_MUTE_TOGGLE:
        {
            if (TRINNOV_IS_MUTED)
            {
                // Unmute
                Log.Debug("Unmute sent");
                //ImageKey(VOL_MUTE_TOGGLE, "vol");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x24, 0x03, 0x00, 0x00, 0x00,
                    0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x7B, 0x22, 0x6D, 0x75, 0x74, 0x65,
                    0x22, 0x3A, 0x66, 0x61, 0x6C, 0x73, 0x65, 0x7D
                }));
            }
            else
            {
                // Mute
                Log.Debug("Mute sent");
                //ImageKey(VOL_MUTE_TOGGLE, "vol-mute");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x23, 0x03, 0x00, 0x00, 0x00,
                    0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x7B, 0x22, 0x6D, 0x75, 0x74, 0x65,
                    0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D
                }));
            }

            TRINNOV_IS_MUTED = !TRINNOV_IS_MUTED;
            break;
        }
        case OPTIMISER_TOGGLE when arg.IsDown:
            ImageKey(OPTIMISER_TOGGLE, "optimiser-press");
            return;
        case OPTIMISER_TOGGLE:
            if (TRINNOV_OPTIMISED)
            {
                // Set unoptimised
                Log.Debug("Optimiser off sent");
                //ImageKey(OPTIMISER_TOGGLE, "optimiser-off");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x26, 0x03, 0x00, 0x00, 0x00,
                    0x12, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x2F, 0x7B, 0x22, 0x62, 0x79, 0x70,
                    0x61, 0x73, 0x73, 0x22, 0x3A, 0x74, 0x72, 0x75,
                    0x65, 0x7D
                }));
            }
            else
            {
                // Set Optimiser on
                Log.Debug("Optimiser on sent");
                //ImageKey(OPTIMISER_TOGGLE, "optimiser-on");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x27, 0x03, 0x00, 0x00, 0x00,
                    0x12, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x2F, 0x7B, 0x22, 0x62, 0x79, 0x70,
                    0x61, 0x73, 0x73, 0x22, 0x3A, 0x66, 0x61, 0x6C,
                    0x73, 0x65, 0x7D
                }));
            }

            TRINNOV_OPTIMISED = !TRINNOV_OPTIMISED;
            break;
        case INPUT_TOGGLE when arg.IsDown:
            return;
        case INPUT_TOGGLE:
        {
            if (!TRINNOV_IS_ANALOG)
            {
                // Set SSL (Analog 1 + 2)
                Log.Debug("Analog 1 + 2 sent");
                ImageKey(INPUT_TOGGLE, "analog");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x34, 0x03, 0x00, 0x00, 0x00,
                    0x14, 0x2F, 0x6D, 0x6F, 0x6E, 0x69, 0x74, 0x6F,
                    0x72, 0x69, 0x6E, 0x67, 0x2F, 0x63, 0x6F, 0x6E,
                    0x74, 0x72, 0x6F, 0x6C, 0x2F, 0x7B, 0x22, 0x6D,
                    0x61, 0x69, 0x6E, 0x5F, 0x73, 0x65, 0x6C, 0x65,
                    0x63, 0x74, 0x65, 0x64, 0x5F, 0x73, 0x6F, 0x75,
                    0x72, 0x63, 0x65, 0x73, 0x22, 0x3A, 0x34, 0x7D
                }));
            }
            else
            {
                // Set PC (Digital)
                Log.Debug("Digital sent");
                ImageKey(INPUT_TOGGLE, "digital");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x34, 0x03, 0x00, 0x00, 0x00,
                    0x14, 0x2F, 0x6D, 0x6F, 0x6E, 0x69, 0x74, 0x6F,
                    0x72, 0x69, 0x6E, 0x67, 0x2F, 0x63, 0x6F, 0x6E,
                    0x74, 0x72, 0x6F, 0x6C, 0x2F, 0x7B, 0x22, 0x6D,
                    0x61, 0x69, 0x6E, 0x5F, 0x73, 0x65, 0x6C, 0x65,
                    0x63, 0x74, 0x65, 0x64, 0x5F, 0x73, 0x6F, 0x75,
                    0x72, 0x63, 0x65, 0x73, 0x22, 0x3A, 0x35, 0x7D
                }));
            }

            TRINNOV_IS_ANALOG = !TRINNOV_IS_ANALOG;
            break;
        }
        case REFERENCE_LEVEL when arg.IsDown:
            ImageKey(REFERENCE_LEVEL, "ref-press");
            return;
        case REFERENCE_LEVEL:
            Log.Debug("Ref sent");
            ImageKey(REFERENCE_LEVEL, "ref");
            Task.Run(() => client.Send(new byte[]
            {
                0x00, 0x00, 0x00, 0x2C, 0x03, 0x00, 0x00, 0x00,
                0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                0x6D, 0x65, 0x7B, 0x22, 0x76, 0x6F, 0x6C, 0x75,
                0x6D, 0x65, 0x5F, 0x72, 0x65, 0x63, 0x61, 0x6C,
                0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D
            }));
            break;
        case DIM_TOGGLE when arg.IsDown:
            ImageKey(DIM_TOGGLE, "dim-press");
            return;
        case DIM_TOGGLE:
        {
            if (TRINNOV_IS_DIMMED)
            {
                // Undim
                Log.Debug("Undim sent");
                //ImageKey(DIM_TOGGLE, "dim");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x23, 0x03, 0x00, 0x00, 0x00,
                    0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x7B, 0x22, 0x64, 0x69, 0x6D, 0x22,
                    0x3A, 0x66, 0x61, 0x6C, 0x73, 0x65, 0x7D
                }));
            }
            else
            {
                // dim
                Log.Debug("Dim sent");
                //ImageKey(DIM_TOGGLE, "dim-on");
                Task.Run(() => client.Send(new byte[]
                {
                    0x00, 0x00, 0x00, 0x22, 0x03, 0x00, 0x00, 0x00,
                    0x11, 0x2F, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x69,
                    0x7A, 0x65, 0x72, 0x2F, 0x76, 0x6F, 0x6C, 0x75,
                    0x6D, 0x65, 0x7B, 0x22, 0x64, 0x69, 0x6D, 0x22,
                    0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D
                }));
            }

            TRINNOV_IS_DIMMED = !TRINNOV_IS_DIMMED;
            break;
        }
    }
}

void ImageKey(int key, string path, bool vol = false)
{
    using var image = Image.Load($"buttons/{path}.png");
    if (vol)
    {
        var font = SystemFonts.Get("Arial", CultureInfo.InvariantCulture).CreateFont(22, FontStyle.Bold);

        var xOffset = 41f;
        if (TRINNOV_VOLUME < 0) xOffset -= 7;

        if (TRINNOV_VOLUME <= -10 || TRINNOV_VOLUME >= 10) xOffset -= 6;

        image.Mutate(x =>
            x.DrawText(TRINNOV_VOLUME.ToString(), font, Color.FromRgb(42, 48, 56), new PointF(xOffset, 56)));
    }

    board.SetKeyBitmap(key, KeyBitmap.Create.FromImageSharpImage(image));
}