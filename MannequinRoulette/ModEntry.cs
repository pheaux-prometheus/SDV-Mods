using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GenericModConfigMenu;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace MannequinRoulette
{
    internal class ModEntry : Mod
    {
        private Config config;
        private NetObjectList<Mannequin> mqlist = new();

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.config = this.Helper.ReadConfig<Config>();
            helper.Events.GameLoop.GameLaunched += configSetup;

            bool doRoulette = this.config.doRoulette;
            bool includeFarmer = this.config.includeFarmer;
            bool swapBoots = this.config.swapBoots;
            string mannequinType = this.config.mannequinType;

            helper.Events.GameLoop.DayEnding += mannequincheck;
        }

        private void configSetup(object? sender, GameLaunchedEventArgs e)
        {
            //for configs
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.config = new Config(),
                save: () => this.Helper.WriteConfig(this.config)
            );

            // bool for roulette
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("activeroulette"),
                tooltip: () => Helper.Translation.Get("activeroulettedesc"),
                getValue: () => this.config.doRoulette,
                setValue: value => this.config.doRoulette = value
            );

            // dropdown for type of mannequin
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("mannequintype"),
                tooltip: () => Helper.Translation.Get("mannequintypedesc"),
                getValue: () => this.config.mannequinType,
                setValue: value => this.config.mannequinType = value,
                allowedValues: new string[] {"Cursed", "Not Cursed", "Both"}
            );

            // bool for include farmer
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("includefarmer"),
                tooltip: () => Helper.Translation.Get("includefarmerdesc"),
                getValue: () => this.config.includeFarmer,
                setValue: value => this.config.includeFarmer = value
            );

            // bool for swap boots
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("swapboots"),
                tooltip: () => Helper.Translation.Get("swapbootsdesc"),
                getValue: () => this.config.swapBoots,
                setValue: value => this.config.swapBoots = value
            );
        }

        /// <summary>
        /// Creates a list of the mannequins in the player's location once day ends
        /// </summary>
        public void mannequincheck(object? sender, DayEndingEventArgs e)
        {
            try
            {
                var player = Game1.player;
                mqlist.Clear();

                foreach (StardewValley.Object value in player.currentLocation.netObjects.Values)
                {
                    if (value is Mannequin mannequin)
                    {
                        mqlist.Add(mannequin);

                        if (this.config.mannequinType == "Cursed" && !mannequin.ItemId.Contains("Cursed"))
                        {
                            mqlist.Remove(mannequin);
                        }
                        else if (this.config.mannequinType == "Not Cursed" && mannequin.ItemId.Contains("Cursed"))
                        {
                            mqlist.Remove(mannequin);
                        }
                    }
                }
                if (mqlist.Count == 0) return;

                if (this.config.doRoulette)
                {
                    mqSwap(mqlist, player);
                    mqlist.Clear();
                }
                else
                {
                    mqlist.Clear();
                    return;
                }

            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed in {nameof(mannequincheck)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mannequins">The valid mannequins (according to config) in the player's location.</param>
        /// <param name="player">Current player.</param>
        public void mqSwap(NetObjectList<Mannequin> mannequins, Farmer player)
        {
            try
            {
                int r = Game1.random.Next(mannequins.Count);
                if (config.includeFarmer)
                {
                    r = Game1.random.Next(mannequins.Count + 1);
                }
                // if including farmer and 'farmer' is chosen, then exit and don't swap
                if (r == mannequins.Count && config.includeFarmer)
                {
                    return;
                }
                else
                {
                    var rm = mannequins[r];
                    rm.hat.Value = player.Equip(rm.hat.Value, player.hat);
                    rm.shirt.Value = player.Equip(rm.shirt.Value, player.shirtItem);
                    rm.pants.Value = player.Equip(rm.pants.Value, player.pantsItem);
                    if (config.swapBoots)
                    {
                        rm.boots.Value = player.Equip(rm.boots.Value, player.boots);
                    }
                    rm.swappedWithFarmerTonight.Value = true;
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed in {nameof(mqSwap)}:\n{ex}", LogLevel.Error);
            }
        }
    }
}
