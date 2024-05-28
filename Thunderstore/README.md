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
- **Repair When Protection Applied**: When resin or tar is applied to a building piece, it will automatically repair itself to full health. This is balanced because technically it's costing more to repair it than if you used your hammer. (Stamina and repair of the hammer doesn't cost anything in the base game.)

## Usage

- **Applying Resin/Tar**: To apply resin or tar to a building piece, you must have the item in your hotbar. Aim at the
  piece you wish to enhance and use the item.
- **Resin Decay**: Over time, the effect of resin will diminish, reflecting its decay. This rate can be configured in
  the server settings if using ServerSync.
- **Water Protection**: Applying tar provides immediate and permanent protection from water damage until it is manually
  removed or altered.

## Damage Reduction Explanation

- **How Resin Reduces Damage**:
    - When resin is applied to a building piece, it proportionally reduces the damage from environmental factors based
      on how much resin is applied. The formula used calculates the damage reduction
      as `damage *= 1.0f - (current resin amount / maximum resin capacity)`.
    - For example, if half the maximum resin is applied, the damage taken by the building piece is reduced by 50%. If
      full resin is applied, it completely negates the damage. The decay system balances this by gradually reducing the
      resin's effectiveness over time.
    - Noteworthy comments about configuring the maximum amount of resin:
        - `If Maximum Resin is Set to 20`
          Increasing the maximum resin capacity to 20 allows for a greater amount of resin to be applied to each
          building
          piece. This change has several effects:
            - Increased Durability: With the maximum resin doubled from the default setting, players can apply more
              resin to each piece. This leads to a potential reduction in damage taken that can scale up to 100% when
              fully applied. For example, if a piece is fully resined with 20 units, it could potentially receive no
              damage from typical sources as the damage
              reduction formula is damage *= 1.0f - (current resin amount / maximum resin capacity). With 20 units, the
              formula allows
              for a complete nullification of the typical damage if fully applied.
            - Longer Protection Duration: Because there's more resin on the piece, it will take longer to decay back to
              an unprotected
              state if the decay rate per unit time remains constant. This provides longer lasting protection without
              the need for
              frequent reapplication, which can be especially beneficial in extended gameplay sessions or in server
              settings where
              maintenance might be less frequent.
            - Resource Management: With the ability to hold more resin, each building piece can become a larger sink for
              resources,
              requiring more planning and gathering by players. This could add an additional layer of resource
              management strategy to
              the game.
        - `If Maximum Resin is Set to 5` Reducing the maximum resin capacity to 5 means that each building piece can
          hold less resin, which affects gameplay in
          the following ways:
            - Reduced Maximum Durability Boost: With a lower cap, the maximum damage reduction any building piece can
              achieve is less.
              At full application (5 units of resin), the building piece will only mitigate up to 50% of incoming damage
              using the
              same damage calculation formula. This makes building pieces less robust against attacks and environmental
              damage
              compared to a higher resin cap.
            - Frequent Reapplication Needed: Since the total amount of resin that can be applied is less, and if the
              decay rate
              remains unchanged, the resin will deplete more quickly. This means players will need to reapply resin more
              frequently to
              maintain even the reduced level of protection, increasing maintenance demands.
            - Less Resource Intensive: On the flip side, reaching the maximum resin application is quicker and requires
              fewer
              resources. This can be advantageous in early game stages or for casual players who prefer less intensive
              resource
              gathering.

- **Why Use Tar**:
    - While resin enhances durability by reducing the damage taken from environmental factors, tar provides a different
      type of protection.
    - Tar completely protects building pieces from all forms of water damage. This includes damage from rain or being
      submerged in water, making it essential for structures in or near water bodies.
    - Using tar is especially beneficial in wet environments or during storms where water exposure is continuous and
      inevitable.

## Likely to ask questions:
- `Will you get your tar and resin back if destroyed?` Yes, you get your tar and resin back if you remove the building piece.
- `Can you apply resin and tar to the same building piece?` Yes, you can apply both to the same building piece.
- `Can you apply resin and tar to the same building piece multiple times?` Yes and no, you can apply both, but only resin multiple times. Tar is a permanent application until removal of the building piece.
- `Can you apply resin and tar to all building pieces?` Yes, you can apply resin and tar to all building pieces that have a WearNTear component. However, if it is already hoverable, it will not apply to it due to how I've implemented it currently.

## Configuration

- **Decay Time**: Set the time it takes for resin to fully decay. Default is 3600 seconds (1 hour).
- **Enable Visual Updates**: Enable or disable the visual updates that show the resin and tar on the building pieces. Default is On.
- **Max Resin**: Set the maximum amount of resin that can be applied to a building piece. Default is 10. The 10 is balanced to increase the health of the building piece by 100% at full capacity.
- **Repair When Protection Applied**: Enable or disable the automatic repair of building pieces when resin or tar is applied. Default is On.
- **Resin Color**: Set the color of the resin visual effect. Default is Yellow.
- **Tar Color**: Set the color of the tar visual effect. Default is Gray.

## Example YAML
    
 ```yaml
# Resin and Tar Exclusion List
# Add the PrefabName of pieces to exclude from Resin or Tar protection visuals.

Resin:
- PrefabNameToExclude1
- PrefabNameToExclude2

Tar:
- PrefabNameToExclude1
- PrefabNameToExclude2
```


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