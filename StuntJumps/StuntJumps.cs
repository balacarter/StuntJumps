using System;
using System.Timers;
using System.Windows.Forms;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;
using NativeUI;

namespace StuntJumps // IMPORTANT REPLACE THIS WITH YOUR MODS NAME
{

    
    public class StuntJumps : Script
    {

        const int ONE_SECOND = 100;         //Seconds in MS
        const int DEGREES_NEEDED = 300;     //Degrees needed to rotated per exta multiplier
        const int SCORE_THRESHOLD = 180;    //Score needed before displaying jump info
        const int DEGREE_THRESHOLD = 10;    //Degrees rotated before counting rotation, this is to prevent micro adjustments adding up

        /// These variables are for Mod Information on startup
        bool firstTime = true;
        string ModName = "Stunt Jumps";
        string Developer = "scriptHijo";
        string Version = "1.0";
        string stats;

        //Native UI vars
        MenuPool _menuPool;
        TimerBarPool _barPool;

        TextTimerBar timer;

        ScriptSettings config;
        Keys OpenMenu;
        UIMenu mainMenu;


        Player player = Game.Player;
        Ped ped = Game.Player.Character;
        Vehicle veh;

        System.Timers.Timer gameTimer;

        float startSpeed;
        Vector3 startLoc;
        Vector3 endLoc;

        float maxYaw = 0;
        float maxPitch = 0;
        float maxRoll = 0;

        float pitchRot = 0;
        float yawRot = 0;
        float rollRot = 0;

        float degRotated;
        float distanceInAir;
        float multiply;

        int score;
        int totalScore;
        int highScore = 0;
        int lastScore = 0;


        bool countdown = false;
        bool fresh = true;
        bool start = false;
        bool validVehicle;
        bool second = false;

        int timeMode = 0;
        int ticks = 0;
        int gameCountdown = 0;
        int countdownSeconds = 3;

        

        public StuntJumps()
        {
            Tick += onTick;
            KeyDown += onKeyDown;
            Interval = 1;

           
            

            //Native UI 
            _menuPool = new MenuPool();
            _barPool = new TimerBarPool();

            mainMenu = new UIMenu("Stunt Jumps Mod", "by ~g~scriptHijo");

            _menuPool.Add(mainMenu);
            gameMode(mainMenu);
            _menuPool.RefreshIndex();

            OpenMenu = Keys.F7;

        }

        private void onTick(object sender, EventArgs e)
        {
            // Mod info
            if (firstTime)
            {
                UI.Notify(ModName + " " + Version + " by " + Developer + " Loaded");
                firstTime = false;
            }

            _menuPool.ProcessMenus();
            
            if(start == true && gameCountdown > 0)
            {
                if(second)
                {
                    if(countdown)
                    {
                        if (countdownSeconds > 0)
                        {
                            BigMessageThread.MessageInstance.ShowOldMessage(countdownSeconds.ToString(), 1000);
                            countdownSeconds--;
                        }
                        else
                        {
                            BigMessageThread.MessageInstance.ShowOldMessage("GO!", 1000);
                            countdown = false;
                            countdownSeconds = 3;
                        }
                            
                        
                    }
                    else
                    {
                        gameCountdown -= 1;
                        _barPool.Remove(timer);
                        timer = new TextTimerBar("TIME LEFT: ", gameCountdown.ToString());
                        _barPool.Add(timer);
                    }


                    second = false;
                }

                _barPool.Draw();

                if (gameCountdown == 0)
                {
                    BigMessageThread.MessageInstance.ShowSimpleShard("TIMES UP!", "Score: " + totalScore + ", Best Score: " + highScore, 5000);
                    resetGame();
                }
            }

            veh = ped.CurrentVehicle;
            validVehicle = ped.IsInVehicle() && veh.ClassType == VehicleClass.Motorcycles;

            if (validVehicle)
            {
                float roll = Function.Call<float>(Hash.GET_ENTITY_ROLL, veh);
                float pitch = Function.Call<float>(Hash.GET_ENTITY_PITCH, ped);
                Vector3 rotVect = Function.Call<Vector3>(Hash.GET_ENTITY_ROTATION, veh, false);
                float yaw = rotVect.Z;



                if (Function.Call<bool>(Hash.IS_ENTITY_IN_AIR, veh))
                {

                    if (fresh)
                    {
                        maxYaw = yaw;
                        maxPitch = pitch;
                        maxRoll = roll;

                        startSpeed = veh.Speed;
                        startLoc = ped.Position;
                        fresh = false;
                    }


                    if (changeInDegrees(maxPitch, pitch) > DEGREE_THRESHOLD)
                    {
                        pitchRot += changeInDegrees(maxPitch, pitch);
                        maxPitch = pitch;
                    }


                    if (changeInDegrees(maxYaw, yaw) > DEGREE_THRESHOLD)
                    {
                        yawRot += changeInDegrees(maxYaw, yaw);
                        maxYaw = yaw;
                    }


                    if (changeInDegrees(maxRoll, roll) > DEGREE_THRESHOLD)
                    {
                        rollRot += changeInDegrees(maxRoll, roll);
                        maxRoll = roll;
                    }


                    degRotated = (pitchRot + yawRot + rollRot);

                    if ((int)degRotated / DEGREES_NEEDED <= 1)
                    {
                        multiply = 1;
                    }
                    else
                    {
                        multiply = (int)degRotated / DEGREES_NEEDED;
                        
                    }



                    score = (int)degRotated;

                    if (score > SCORE_THRESHOLD)
                    {
                        BigMessageThread.MessageInstance.ShowOldMessage("~g~" + multiply + "x ~w~ " + score, 250);

                        
                    }

                }
                else
                {
                    if (!fresh)
                    {
                        Wait(250);
                        if (ped.IsInVehicle() && veh.HeightAboveGround < 0.9 && score > SCORE_THRESHOLD)
                        {
                            endLoc = ped.Position;
                            distanceInAir = GTA.Math.Vector3.Distance(startLoc, endLoc);
                            score = score * (int)multiply * (int)(distanceInAir / 10);
                            totalScore += score;
                            
                            if (score > highScore)
                            {
                                BigMessageThread.MessageInstance.ShowOldMessage("High Score!\n~w~Score: ~g~" + score, 2000);
                                
                                highScore = score;
                                Wait(500);
                            }
                            else
                            {
                                BigMessageThread.MessageInstance.ShowOldMessage("~w~Score: ~g~" + score, 2000);
                            }

                            lastScore = score;
                            stats = "High Score: ~b~" + highScore.ToString() + "~s~ Total Score: ~g~" + totalScore.ToString() 
                                    + "     ~s~Last Score: ~y~" + lastScore.ToString() + "~s~ Last Distance: " + (int)distanceInAir + "ft";
                            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
                            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, stats);
                            Function.Call(Hash._0x238FFE5C7B0498A6﻿, 0, 0, 0, -1);

                            //UI.Notify("Distance: " + (int)distanceInAir + "ft");
                            //UI.Notify("Score: ~b~" + score);
                            //UI.Notify("High Score: ~g~" + highScore);
                            //UI.Notify("Total Score: ~y~" + totalScore);

                            resetJump();


                        }
                        else if (ped.IsInVehicle() && veh.HeightAboveGround > 0.9)
                        {

                        }
                        else
                        {
                            resetJump();
                        }
                    }

                }

            }
            else
            {
                resetJump();
            }

        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == OpenMenu && !_menuPool.IsAnyMenuOpen()) // Our menu on/off switch
                mainMenu.Visible = !mainMenu.Visible;
        }

        private float changeInDegrees(float oldDegree, float newDegree)
        {

            return Math.Abs(Math.Abs(oldDegree) - Math.Abs(newDegree));
        }

        private bool flipped(float prev, float cur)
        {
            if (prev < 0 && cur > 0)
                return true;
            else if (cur < 0 && prev > 0)
                return true;
            else return false;
        }

        public void resetGame()
        {
            pitchRot = 0;
            yawRot = 0;
            rollRot = 0;
            degRotated = 0;
            score = 0;
            totalScore = 0;
            highScore = 0;
            multiply = 0;
            distanceInAir = 0;
            fresh = true;
            start = false;
        }
        public void resetJump()
        {
            pitchRot = 0;
            yawRot = 0;
            rollRot = 0;
            fresh = true;
        }

        //*********************************Start native UI functions*********************************\\
        public void gameMode(UIMenu menu)
        {


            var times = new List<dynamic>();
            times.Add("Free Mode");

            
            for(int i = 5; i <= 120; i+=5)
            {
                times.Add(i.ToString());
            }

            UIMenuListItem gameTimeMode = new UIMenuListItem("Time", times, 0);

            menu.AddItem(gameTimeMode);
            menu.OnListChange += (sender, item, index) =>
            {
                if (item == gameTimeMode)
                {
                    if(index == 0)
                        timeMode = index;
                    else
                        timeMode = index*5;
                }
            };

            UIMenuItem startGame = new UIMenuItem("Start");
            menu.AddItem(startGame);
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == startGame)
                {
                    resetGame();

                    if (ped.IsInVehicle())
                    {
                        if (timeMode != 0)
                        {
                            gameCountdown = timeMode;
                            gameTimer = new System.Timers.Timer(1000);
                            gameTimer.Elapsed += OnTimedEvent;
                            gameTimer.AutoReset = true;
                            gameTimer.Enabled = true;

                            _barPool.Remove(timer);
                            timer = new TextTimerBar("TIME LEFT:", gameCountdown.ToString());
                            _barPool.Add(timer);
                            countdown = true;
                        }


                        start = true;

                        menu.Visible = false;
                    }
                    else UI.Notify("Enter a Motorbike to start");

                }
            };

        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (gameCountdown > 0)
            {
                second = true;
            }
            else gameTimer.Enabled = false;
        }



    }


   
}
