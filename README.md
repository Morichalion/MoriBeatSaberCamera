# Morichalion's Lazy Streamer Beat Saber Camera for LIV Avatars.

# Description
With this script selected, the camera will switch from a standard selfie-style camera behavior to a smooth, third-person camera set of behaviors when the streamer selects a 90 or 360 degree stage.

There's also an overlay for your current score, and a way for twitch viewers to change your game camera in real time. 

Next thing on the agenda is setting up a straight-forward unity project to smooth out overlay production.

# Installation
1. Ensure that you have the sdkv2_compositor version of LIV installed.

2. Extract the .zip to ~/Documents/LIV/Plugins/CameraBehaviours

3. Ensure that HTTP status is installed for Beat Saber. Over here: https://github.com/opl-/beatsaber-http-status

5. Run LIV, select Plugins -> 'Mori's Beat Saber Cam'.

6. Review the config file (if you want the overlay and/or twitch commands). The current "default" camera is the "RigCam" ones. 
 
6. Profit. 

# Configuration
The config file is created at ~Documents\LIV\Plugins\CameraBehaviours\MoriBScam\ini.txt on the first run of the plugin. Directions should be printed in a fairly easy-to-understand way.

The overlay and twitch integration is off by default. Make the appropriate changes to the ini.txt file to enable these features. 


# Usage
Twitch command currently require broadcaster, moderator, or VIP status to use. Those commands are: 

!cam fp

!cam follow

!cam menu
