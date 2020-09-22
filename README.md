
# SE-Battery-Monitor
A small in-game script for use in the Programmable Block in the game : [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/), whose purpose is to monitor batteries and provide a small subset of actions based on storage capacity and docking states.

## Install via Steam Workshop
To install this script, please visit the workshop page for the script [here](https://steamcommunity.com/sharedfiles/filedetails/?id=1940618382)  and subscribe to the workshop item. The script should then appear when browsing for scripts on the programmable block.

## Introduction

Ever wanted to know the state of your battery capacity and charge status of your base/ship? Ever wanted to know how long you have to keep your ship docked until it's fully charged or precisely how much running time you have left?  
  
Well good news, you can do manually clicking between each and every battery checking it's Capacity, its input and output then adding it all together and calculate the necessary information from it! Boring right? Not only boring but also time consuming!  
  
Ever wanted to switch your batteries into charge mode on your docked ship and some of your base into discharge mode so that your ship would charge faster?  
  
Well good news! You can do this manually by switching between each battery and changing their modes! Wow, this won't be a quick task at all! :(  
  
Not anymore! This script does it for you!
  
## Features :  
1. Collects data on *all* batteries.  
2. Calculates Current charge, max charge, input, max input, output, max input, time until full charge, time until discharge of the local grid.  
3. Allows you to output all or one of the above data on an LCD or cockpit screen. (By default shows all of the information on the Program block screen)  
4. Allows you to specify behavioural changes of blocks that can be toggled depending on current charge capacity, input capacity or output capacity... requires the use of grouping (see below)  
5. Every battery will have a percentage appended at the end of it's name so that you can see the capacity of individual containers without opening them  
6. Allows you to set batteries to automatically change to discharge on a base grid when a ship grid connects to it.  
	- Likewise ships can be automatically changed to recharge mode when connected to a base grid.  
	- Calculates the maximum Input the connected ship, sets enough batteries to discharge mode to see to the ships needs so that the ship only causes the base to set relevant batteries to discharge mode. I.e. if the ship has a max input of 400Watts, the base will set enough batteries to discharge mode (output of 400Watts or greater). THE BASE will always reserve 50% of the batteries for itself.  
	- Ship will keep the healthiest battery in auto mode, whilst others charge, this is so that if the ship unexpectedly disconnects, the battery will keep the ship afloat (assuming engines are on) rather than it plummeting to the floor (assuming you have it docked to the ceiling) whilst the program block restores all other batteries to normal running mode.  
7. Supports [Fluid batteries](https://steamcommunity.com/sharedfiles/filedetails/?id=1677937818)!
  
## How to use   
  
Create a Program block. Install this script. `[BatteryMonitor]` will be appended into it's name, this is an indicator that the script is working, the summary of your grids storage will be shown on the program block and it's screen.  
  
------------------------------------------------------------------------  
#### LCDs :  
------------------------------------------------------------------------  

You may choose **ONE** of the tags below to add to an LCD  
Either put the tag in the Custom Data section or straight into the name (end of name recommended, Custom Data preferred) of the LCD.  
  
**[BatteryPower]** : The current charge of the local grid.  
**[BatteryMaxPower]** : The maximum charge of the local grid.  
**[BatteryInput]** : The current input of the grid.  
**[BatteryMaxInput]** : The maximum input the grid can support.  
**[BatteryOutput]** : The current output of the grid.  
**[BatteryMaxOutput]** : The maximum output the grid can support.  
**[BatterySummary]** : Shows all of the above information.  
**[BatteryStatus]** : Same as [BatterySummary].  
**[BatteryChargeSummary]** : A summary displaying whether or not the grid is currently charging or discharging, how long until full charge or full discharge.  
**[BatteryChargeStatus]** : Same as [BatteryChargeSummary].  
  
------------------------------------------------------------------------  
#### Cockpit Screens:  
------------------------------------------------------------------------    
Same as above with LCD's however after the tag append a colon ( : ) followed by a number, this number represent the screen that this data should be shown on. This number starts from 0. I.e. [BatterySummary]:0 would show the battery summary on the first screen in the cockpit while [BatteryOutput]:3 would show the output on the fourth screen.
  
  ------------------------------------------------------------------------  
#### Ignoring Batteries/Devices:  
------------------------------------------------------------------------  

Add the tag **[BatteryIgnore]** to either the Name or Custom Data and the script will not touch this device.
  
  ------------------------------------------------------------------------  
#### Automatic battery management:  
------------------------------------------------------------------------  
  
So you want ships to set themselves to recharge mode when they dock, and the base to to set enough batteries to discharge to support that ship and thereby charge the ship as fast as possible?  
  
Easy task!  
  
Add the tag **[AutoRecharge]** to either the Name or Custom Data of the program block of the ship.  
Add the tag **[AutoDischarge]** to either the Name or Custom Data of the program block of the base.  
  
The script will now take care of the rest.

------------------------------------------------------------------------    
#### Selective Grouping:  
------------------------------------------------------------------------  
  
*So you want to track **SPECIFIC** batteries instead of all batteries?*  
No problem add the tag `[BatteryResponsibility]` to the container **AND** program block to show that you are specifying a new Responsibility/Group.  
  
Follow this tag up with a colon ( **:** ) and a tag of your choosing. I.E. `[BatteryResponsibility]:[Test]` would tag the batteries and script as `[Test]` and the script will only track the batteries with the Test tag.  
  
This can benefit you if you want to set up different scripts for different batteries i.e. seeing states on vanilla batteries vs fluid batteries.
  
  
------------------------------------------------------------------------  
#### Turning on/off miscellaneous blocks in a group:  
  ------------------------------------------------------------------------  
  

Scenario: You have some lighting set up around the base, you have several lights which look like hazard lights but are red near your batteries area which you want to turn on when batteries are nearly empty indicating the need to charge them either by adding more reactors or turning off power hogs.  
  
1. First set up your light as you wish.  
2. Add `[BatteryMonitor]` as a tag.  
3. Add the device to the selective grouping group. I.e. `[BatteryResponsibility]:[Test]  `
4. Choose and add **ONE** of the following tags :  
  
------------------------------------------------------------------------   
##### Capacity
------------------------------------------------------------------------  
**[OffWhenTotalCapacityFull]** : Turn OFF device when grid batteries are at or above 98%. Otherwise device is ON.  
**[OnWhenTotalCapacityFull]** : Same as above, but inverted.  

**[OffWhenTotalCapacityMoreThan]:###** : Turn OFF when grid batteries are above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalCapacityMoreThan]:###** : Same as above, but inverted.  
  
**[OffWhenTotalCapacityMoreThanEqualTo]:###** : Turn OFF when grid batteries are at (equal to) or above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalCapacityMoreThanEqualTo]:###** : Same as above, but inverted.  

  ------------------------------------------------------------------------    
##### Input
  ------------------------------------------------------------------------  
  
**[OffWhenTotalInputFull]** : Turn OFF device when grid input is at or above 98%. Otherwise device is ON.  
**[OnWhenTotalInputFull]** :  Same as above, but inverted.

**[OffWhenTotalInputMoreThan]:###** : Turn OFF when grid power input is above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalInputMoreThan]:###** : Same as above, but inverted.  
  
**[OffWhenTotalInputMoreThanEqualTo]:###** : Turn OFF when grid power input is at (equal to) or above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalInputMoreThanEqualTo]:###** : Same as above, but inverted.  

  ------------------------------------------------------------------------   
##### Output
  ------------------------------------------------------------------------ 

**[OffWhenTotalOutputFull]** : Turn OFF device when grid input is at or above 98%. Otherwise device is ON.  
**[OnWhenTotalOutputFull]** : Same as above, but inverted.
  
**[OffWhenTotalOutputMoreThan]:###** : Turn OFF when grid power output is above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalOutputMoreThan]:###** : Same as above, but inverted.  
  
**[OffWhenTotalOutputMoreThanEqualTo]:###** : Turn OFF when grid power output is at (equal to) or above the specified value as a percentage % *(to specify a value, replace ### with a number)*. Otherwise the device is ON.  
**[OnWhenTotalOutputMoreThanEqualTo]:###** : Same as above, but inverted.  
  
  
------------------------------------------------------------------------  
#### Default grouping:  
------------------------------------------------------------------------  
  
If you have not specified a group then the group is set to **[Default]** to interact with this group/responsibilty you can do so with `[BatteryResponsibility]:[Default]`.  This saves time and hassle if your intent is to use this script to track ALL batteries, you can just use the Default group/responsibility.
  
Using the above exmaple regarding the lights, the lights will have to include `[BatteryResponsibility]:[Default]` as they are a miscellaneous block and won't be controlled automatically. However, the programmable block, cockpits, lcd's and batteries will be controlled/assigned to the Default group automatically and will not have to be assigned to the group.

------------------------------------------------------------------------  
# License: 

This script utilises the [MIT License](https://github.com/Krelsis/SE-Battery-Monitor/blob/master/LICENSE)
