// ============================================================================
// Nova. (c) 2008 Ken Reed
//
// This file contains data that are persistent across multiple invokations of
// dialogs and even muliple inovcations of the GUI itself. 
//
// This is free software. You can redistribute it and/or modify it under the
// terms of the GNU General Public License version 2 as published by the Free
// Software Foundation.
// ============================================================================

using NovaCommon;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System;


// ============================================================================
// Manipulation of data that is persistent across muliple invocations of the
// GUI. Some things don't need to be persistent but it's convenient to keep all
// "global" data in one place.
// ============================================================================

namespace Nova
{
   [Serializable]
   public sealed class GuiState
   {


// ============================================================================
// Data used by the GUI that will be persistent across multiple program
// invocations.
// ============================================================================

      public ArrayList    DeletedDesigns           = new ArrayList();
      public ArrayList    DeletedFleets            = new ArrayList();
      public ArrayList    Messages                 = new ArrayList();
      public Intel   InputTurn                = null;
      public Hashtable    BattlePlans              = new Hashtable();
      public Hashtable    KnownEnemyDesigns        = new Hashtable();
      public Hashtable    PlayerRelations          = new Hashtable();
      public Hashtable    StarReports              = new Hashtable();
      public Hashtable    AvailableComponents      = new Hashtable();
      public List<Fleet>  PlayerFleets             = new List<Fleet>();
      public List<Star>   PlayerStars              = new List<Star>();
      public Race         RaceData                 = new Race();
      public TechLevel    ResearchLevel            = new TechLevel();
      public TechLevel    ResearchResources        = new TechLevel();
      public bool         FirstTurn                = true;
      public double       ResearchAllocation       = 0;
      public int          ResearchBudget           = 10;
      public int          TurnYear                 = 0; 
      public string       GameFolder               = null;
      public string       RaceName                 = null;
      public string       ResearchTopic            = "Energy";


// ============================================================================
// Data private to this module.
// ============================================================================

      private static GuiState        Instance      = null;
      private static BinaryFormatter Formatter     = new BinaryFormatter();
      private static string          StatePathName = null;


// ============================================================================
// Private constructor to prevent anyone else creating instances of this class.
// ============================================================================

      private GuiState() {}


// ============================================================================
// Provide a mechanism of accessing the single instance of this class that we
// will create locally. Access to the data is thread-safe.
// ============================================================================

      public static GuiState Data
      {
         get {
            Object padlock = new Object();

            lock(padlock) {
               if (Instance == null) {
                  Instance = new GuiState();
               }
            }
            return Instance;
         }

         set {
            Instance = value;
         }
      }

      // ============================================================================
      // Initialise the data needed for the GUI to run.
      // On first run a password is required , on any additional turnsb no password 
      // is required.
      // ============================================================================

      public static void Initialize(bool bPasswordNeeded)
      {
          // ----------------------------------------------------------------------------
          // Get the name of the folder where all the game files will be stored. This
          // is placed in the Registry by the Race Generator and we cache a copy in the
          // GUI state data store.
          //
          // Note if the same folder on the same machine is chosen for all Nova GUI and
          // Nova Console files it is possible to play a game with multiple races using
          // the same log-in name. This is a useful debugging aid.
          // ----------------------------------------------------------------------------

          RegistryKey regKey = Registry.CurrentUser;
          RegistryKey regSubKey = regKey.CreateSubKey(Global.RootRegistryKey);
          string gameFolder = "none";
          try
          {
              gameFolder = regSubKey.GetValue(Global.ClientFolderKey).ToString();
          }
          catch
          {
              Report.FatalError("An expected registry entry is missing\n" +
                                "Have you ran the Race Designer and \n" +
                                "Nova Console?");
          }

          // ----------------------------------------------------------------------------
          // Load the game persistent data and all component details and check the
          // password.
          // ----------------------------------------------------------------------------
          if (bPasswordNeeded) //i.e. the first time the gui loads select a race..
          {
              GuiState.Restore(gameFolder);
          }
          else
          {
              GuiState.Restore(gameFolder, GuiState.Data.RaceName);
          }

          if (AllComponents.Restore() == false)
          {
              Report.FatalError("Could not find component definition file.");
          }

          if (GuiState.Data.FirstTurn)
          {
              GuiState.Data.BattlePlans.Add("Default", new BattlePlan());
              ProcessRaceDefinition();
          }

          // ----------------------------------------------------------------------------
          // Check the password for access to this race's data
          // ----------------------------------------------------------------------------

          ControlLibrary.CheckPassword password =
             new ControlLibrary.CheckPassword(GuiState.Data.RaceData);

          if (bPasswordNeeded)
          {
              password.ShowDialog();
              password.Dispose();

              if (password.DialogResult == DialogResult.Cancel)
              {
                  System.Threading.Thread.CurrentThread.Abort();
              }
          }

          LoadTurnFile();

          if (GuiState.Data.FirstTurn)
          {
              foreach (string raceName in GuiState.Data.InputTurn.AllRaceNames)
              {
                  GuiState.Data.PlayerRelations[raceName] = "Neutral";
              }
          }

          GuiState.Data.FirstTurn = false;
      }


      // ============================================================================
      // Read and process the Global Turn file (Nova.Intel) generated by the Nova
      // Console. This file must be present before the GUI will run (since it
      // contains all sorts of important data such as a list of all stars which is
      // needed to draw the star map). This is also saved in the GUI state data
      // store.
      //
      // Note that this file is not read again after the first time a new turn is
      // received until a new turn arrives. This is because we will update some of
      // the Global Turn data during the preparation of the next turn.
      // ============================================================================

      public static void LoadTurnFile()
      {
          string turnFileName = Path.Combine(GuiState.Data.GameFolder, "Nova.Intel");

          if (File.Exists(turnFileName) == false)
          {
              Report.FatalError
              ("The Nova GUI cannot start unless a turn file is present");
          }

          FileStream turnFile = new FileStream(turnFileName, FileMode.Open);
          int turnYearInFile = (int)Formatter.Deserialize(turnFile);

          if (turnYearInFile != GuiState.Data.TurnYear)
          {
              GuiState.Data.InputTurn = Formatter.Deserialize(turnFile)
                                        as Intel;
              turnFile.Close();

              GuiState.Data.TurnYear = turnYearInFile;
              GuiState.Data.DeletedFleets.Clear();
              GuiState.Data.DeletedDesigns.Clear();

              IntelReader.ReadIntel();
          }
      }


      // ============================================================================
      // Read the race definition file into the persistent data store. If this is the
      // very first turn of a new game then process it's content to set up initial
      // race parameters (e.g. initial technology levels, etc.).
      // ============================================================================

      private static void ProcessRaceDefinition()
      {
          string raceFileName = Path.Combine(GuiState.Data.GameFolder,
                                             GuiState.Data.RaceName + ".race");

          GuiState.Data.RaceData = new Race(raceFileName);

          MainWindow.nova.Text = "Nova - " + GuiState.Data.RaceData.PluralName;

          ProcessPrimaryTraits();
          ProcessSecondaryTraits();
      }


      // ============================================================================
      // Determine the components available to this race according to the starting
      // tech levels. Race-specific adjustments will be made in ProcessPrimaryTraits.
      // This processing is only performed once per game.
      // ============================================================================

      public static void DetermineAvailableComponents()
      {
          foreach (NovaCommon.Component component in
                   AllComponents.Data.Components.Values)
          {
              if (GuiState.Data.ResearchLevel >= component.RequiredTech)
              {
                  RaceComponents.Add(component, false);
              }
          }
      }


      // ============================================================================
      // ReadIntel the Primary Traits for this race.
      // ============================================================================

      private static void ProcessPrimaryTraits()
      {
          switch (GuiState.Data.RaceData.Traits.Primary.Name)
          {
              case "JackOfAllTrades":
                  GuiState.Data.ResearchLevel = new TechLevel(3);//set all to 3
                  //GuiState.Data.ResearchLevel = new TechLevel(26); //set all to 26
                  //AddComponent("Chameleon Scanner");//obsolete but will be rejected now...
                  break;

              case "HyperExpansion":
                  //GuiState.Data.ResearchLevel = new TechLevel(26);
                  GuiState.Data.ResearchLevel = new TechLevel(1); // set all to 1
                  AddComponent("Settler's Delight");
                  AddComponent("Mini-Colony Ship");
                  break;
          }
#if (DEBUG)
          // Just for testing
          GuiState.Data.ResearchLevel = new TechLevel(26);
#endif

          DetermineAvailableComponents();
      }


      // ============================================================================
      // Add a named component, will now check if it exists
      // ============================================================================

      private static void AddComponent(string componentName)
      {
          if (ComponentExists(componentName))
          {
              GuiState.Data.AvailableComponents[componentName] = AllComponents.Data.Components[componentName];
          }
          else
          {
              string s = "Error: The " + componentName + " component does not exist!";
              MessageBox.Show(s);
          }
      }

      private static bool ComponentExists(string componentName)
      {
          return AllComponents.Data.Components.ContainsKey(componentName);
      }

      // ============================================================================
      // ReadIntel the Secondary Traits for this race.
      // ============================================================================

      private static void ProcessSecondaryTraits()
      {
          if (GuiState.Data.RaceData.Traits.Contains("IFE"))
          {
              int level = (int)GuiState.Data.ResearchLevel.TechValues["Propulsion"];
              level++;
              GuiState.Data.ResearchLevel.TechValues["Propulsion"] = level;
          }
          if (GuiState.Data.RaceData.Traits.Contains("ISB"))
          {
              // Improved Starbases gives a 20% discount to starbase hulls.
              foreach (Component component in GuiState.Data.AvailableComponents.Values)
              {
                  // TODO (low priority) - work out why it sometimes is null.
                  if (component == null || component.Type != "Hull") continue;
                  Hull hull = component.Properties["Hull"] as Hull;
                  if (hull == null || !hull.IsStarbase) continue;

                  NovaCommon.Resources cost = component.Cost;
                  cost.Boranium *= 0.8;
                  cost.Ironium *= 0.8;
                  cost.Germanium *= 0.8;
                  cost.Energy *= 0.8;
              }
          }
      }


// ============================================================================
// Restore the GUI persistent data if the state store file exists (it typically
// will not on the very first turn of a new game). Later on, when we read the
// file Nova.Intel we will reset the persistent data fields if the turn file
// indicates the first turn of a new game.
// ============================================================================

      public static void Restore(string gameFolder)
      {

// ----------------------------------------------------------------------------
// Scan the game directory for .race files. If only one is present then that is
// the race we will use (single race test bed or remote server). If more than one is
// present then display a dialog asking the player which race he wants to use
// (multiplayer game with all players playing from a single game directory).
// ----------------------------------------------------------------------------

         DirectoryInfo directory = new DirectoryInfo(gameFolder);
         FileInfo[]    raceFiles = directory.GetFiles("*.race");

         if (raceFiles.Length == 0) {
            Report.FatalError
            ("The Nova GUI cannot start unless a race file is present");
          }

         string raceName = null;

         if (raceFiles.Length > 1) {
            raceName = SelectRace(raceFiles);
         }
         else {
            string pathName = raceFiles[0].FullName;
            raceName = Path.GetFileNameWithoutExtension(pathName);
         }

         StatePathName = Path.Combine(gameFolder, raceName + ".state");

         if (File.Exists(StatePathName)) {
            FileStream stateFile = new FileStream(StatePathName,FileMode.Open);
            GuiState.Data        = Formatter.Deserialize(stateFile)as GuiState;
            stateFile.Close();
         }

         // Copy the race and game folder names into the state data store. This
         // is just a convenient way of making them globally available.

         Data.RaceName   = raceName;
         Data.GameFolder = gameFolder;

         MainWindow.nova.Text = "Nova - " + Data.RaceData.PluralName;
      }


      // ============================================================================
      // Restore the GUI persistent data if the state store file exists (it typically
      // will not on the very first turn of a new game). Later on, when we read the
      // file Nova.Intel we will reset the persistent data fields if the turn file
      // indicates the first turn of a new game.
      // ============================================================================

      public static void Restore(string gameFolder, string raceName)
      {
         StatePathName = Path.Combine(gameFolder, raceName + ".state");

         if (File.Exists(StatePathName))
         {
            FileStream stateFile = new FileStream(StatePathName, FileMode.Open);
            GuiState.Data = Formatter.Deserialize(stateFile) as GuiState;
            stateFile.Close();
         }

         // Copy the race and game folder names into the state data store. This
         // is just a convenient way of making them globally available.

         Data.RaceName = raceName;
         Data.GameFolder = gameFolder;

         MainWindow.nova.Text = "Nova - " + Data.RaceData.PluralName;
      }

// ============================================================================
// Save the GUI global data and flag that we should now be able to restore it.
// ============================================================================

      public static void Save()
      {
         FileStream stateFile = new FileStream(StatePathName,FileMode.Create);
         Formatter.Serialize(stateFile, GuiState.Data);
         stateFile.Close();
      }


// ============================================================================
// Pop up a dialog to select the race to play
// ============================================================================

      private static string SelectRace(FileInfo[] raceFiles)
      {
         SelectRaceDialog raceDialog = new SelectRaceDialog();

         foreach (FileInfo file in raceFiles) {
            string pathName = file.FullName;;
            string raceName = Path.GetFileNameWithoutExtension(pathName);
            raceDialog.RaceList.Items.Add(raceName);
         }

         raceDialog.RaceList.SelectedIndex = 0;
            
         DialogResult result = raceDialog.ShowDialog();
         if (result == DialogResult.Cancel) {
            Report.FatalError
            ("The Nova GUI cannot start unless a race has been selected");
         }

         string selectedRace = raceDialog.RaceList.SelectedItem as string;

         raceDialog.Dispose();
         return selectedRace;
      }

   }
}

