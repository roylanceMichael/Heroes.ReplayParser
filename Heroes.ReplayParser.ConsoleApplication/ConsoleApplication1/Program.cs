﻿using System.IO;
using MpqLib.Mpq;
using System;
using System.Collections.Generic;
using System.Linq;
using Heroes.ReplayParser;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var heroesAccountsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
            var randomReplayFileName = Directory.GetFiles(heroesAccountsFolder, "*.StormReplay", SearchOption.AllDirectories).OrderBy(i => Guid.NewGuid()).First();

            // Use temp directory for MpqLib directory permissions requirements
            var tmpPath = Path.GetTempFileName();
            File.Copy(randomReplayFileName, tmpPath, true);

            try
            {
                // Create our Replay object: this object will be filled as you parse the different files in the .StormReplay archive
                var replay = new Replay();
                MpqHeader.ParseHeader(replay, tmpPath);
                using (var archive = new CArchive(tmpPath))
                {
                    ReplayInitData.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayInitData.FileName));
                    ReplayDetails.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayDetails.FileName));
                    ReplayTrackerEvents.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayTrackerEvents.FileName));
                    ReplayAttributeEvents.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayAttributeEvents.FileName));
                    if (replay.ReplayBuild >= 32455)
                        ReplayGameEvents.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayGameEvents.FileName));
                    ReplayServerBattlelobby.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayServerBattlelobby.FileName));
                    ReplayMessageEvents.Parse(replay, GetMpqArchiveFileBytes(archive, ReplayMessageEvents.FileName));
                    Unit.ParseUnitData(replay);
                }

                // Our Replay object now has all currently available information
                var playerDictionary = new Dictionary<int, Player>();
                Console.WriteLine("Replay Build: " + replay.ReplayBuild);
                Console.WriteLine("Map: " + replay.Map);
                foreach (var player in replay.Players.OrderByDescending(i => i.IsWinner))
                {
                    playerDictionary[player.PlayerId] = player;
                    Console.WriteLine("Player: " + player.Name + ", Win: " + player.IsWinner + ", Hero: " + player.Character + ", Lvl: " + player.CharacterLevel + (replay.ReplayBuild >= 32524 ? ", Talents: " + string.Join(",", player.Talents.OrderBy(i => i)) : ""));
                }


                foreach (var message in replay.ChatMessages)
                     if (playerDictionary.ContainsKey(message.PlayerId))
                        Console.WriteLine(playerDictionary[message.PlayerId].Name + ": " + message.Message);
                    
                Console.WriteLine("Press Any Key to Close");
                Console.Read();
            }
            finally
            {
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);
            }
        }

        private static byte[] GetMpqArchiveFileBytes(CArchive archive, string archivedFileName)
        {
            var buffer = new byte[archive.FindFiles(archivedFileName).Single().Size];
            archive.ExportFile(archivedFileName, buffer);
            return buffer;
        }
    }
}
