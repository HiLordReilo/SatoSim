using System;
using System.IO;
using static SatoSim.Core.Managers.GameManager;

// Setup game directory
GameDirectory = Path.Combine(AppContext.BaseDirectory, "Directory");
if (!Directory.Exists(GameDirectory)) Directory.CreateDirectory(GameDirectory);

// Start the game loop
using var game = new SatoSim.Core.Game1();
game.Run();