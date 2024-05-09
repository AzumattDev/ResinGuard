# Description

## ResinGuard is a Valheim mod that enhances your building experience by allowing you to apply resin and tar to your building pieces. Resin increases the durability of the pieces, making them more resistant to damage. Tar provides complete protection against water damage, whether from rain or being submerged.

![](https://github.com/AzumattDev/ResinGuard/blob/master/Thunderstore/icon.png)

`This mod uses ServerSync. If installed on both the server and all clients, it will synchronize all configurations with the [Synced with Server] tag to the client.`

`This mod includes a file watcher. If the configuration file is changed directly on the server's file system and saved, the changes will automatically sync to all clients.`

While not required on servers, it is highly recommended, to ensure that resin decay and other features are synchronized
across all clients.

---

## TODO:

- Get feedback from players on how they'd like this mod to work, implement changes based on feedback. Mostly implemented
  something that "feels right" at the moment.
- Probably add filtering for what this applies to, for now it's fine. It applies to everything with a WearNTear
  component. However, if it is already hoverable, it will not apply to it due to how I've implemented it currently.
- Maybe create an item that applies resin to the object? Instead of using the UseItem method that is already in place.
- More configs?

## Features

- **Resin Application**: Apply resin to building pieces to increase their durability.
- **Tar Application**: Apply tar to make building pieces completely resistant to water damage.
- **Decay Mechanism**: Resin will gradually decay over time, which can be configured in the settings.

## Usage

- **Applying Resin/Tar**: To apply resin or tar to a building piece, you must have the item in your hotbar. Aim at the
  piece you wish to enhance and use the item.
- **Resin Decay**: Over time, the effect of resin will diminish, reflecting its decay. This rate can be configured in
  the server settings if using ServerSync.
- **Water Protection**: Applying tar provides immediate and permanent protection from water damage until it is manually
  removed or altered.

## Likely to ask questions:
- `Will you get your tar and resin back if destroyed?` Yes, you get your tar and resin back if you remove the building piece.
- `Can you apply resin and tar to the same building piece?` Yes, you can apply both to the same building piece.
- `Can you apply resin and tar to the same building piece multiple times?` Yes and no, you can apply both, but only resin multiple times. Tar is a permanent application until removal of the building piece.
- `Can you apply resin and tar to all building pieces?` Yes, you can apply resin and tar to all building pieces that have a WearNTear component. However, if it is already hoverable, it will not apply to it due to how I've implemented it currently.

## Configuration

- **Decay Time**: Set the time it takes for resin to fully decay. Default is 3600 seconds (1 hour).

<details>
<summary><b>Installation Instructions</b></summary>

***You must have BepInEx installed correctly! I can not stress this enough.***

### Manual Installation

`Note: (Manual installation is likely how you have to do this on a server, make sure BepInEx is installed on the server correctly)`

1. **Download the latest release of BepInEx.**
2. **Extract the contents of the zip file to your game's root folder.**
3. **Download the latest release of ResinGuard from Thunderstore.io.**
4. **Extract the contents of the zip file to the `BepInEx/plugins` folder.**
5. **Launch the game.**

### Installation through r2modman or Thunderstore Mod Manager

1. **Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
   or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).**

   > For r2modman, you can also install it through the Thunderstore site.
   ![](https://i.imgur.com/s4X4rEs.png "r2modman Download")

   > For Thunderstore Mod Manager, you can also install it through the Overwolf app store
   ![](https://i.imgur.com/HQLZFp4.png "Thunderstore Mod Manager Download")
2. **Open the Mod Manager and search for "ResinGuard" under the Online
   tab. `Note: You can also search for "Azumatt" to find all my mods.`**

   `The image below shows VikingShip as an example, but it was easier to reuse the image.`

   ![](https://i.imgur.com/5CR5XKu.png)

3. **Click the Download button to install the mod.**
4. **Launch the game.**

</details>

<br>
<br>

`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/qhr2dWNEYq)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>