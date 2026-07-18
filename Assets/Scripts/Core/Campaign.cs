using System;

namespace SuperSniper8D
{
    /// <summary>A single authored contract.</summary>
    [Serializable]
    public class MissionDef
    {
        public string name;
        public string intel;
        public int targets;
        public int movers;
        public float moverSpeed;
        public float timeLimit;
    }

    /// <summary>A region: a themed cluster of contracts, unlocked as a block.</summary>
    [Serializable]
    public class RegionDef
    {
        public string name;
        public string tagline;
        public MissionDef[] missions;
    }

    /// <summary>
    /// The authored campaign: hand-written regions and contracts, replacing the
    /// three throwaway procedural levels. Difficulty escalates within and across
    /// regions. Add regions/missions here — the home screen, mission select and
    /// unlock gating all read from this data automatically.
    /// </summary>
    public static class Campaign
    {
        public static RegionDef[] Build()
        {
            return new[]
            {
                new RegionDef
                {
                    name = "OLD HARBOUR",
                    tagline = "Fog, cranes, and a smuggling ring that owns the docks.",
                    missions = new[]
                    {
                        M("ROOFTOP OVERWATCH", "TARGET: arms courier. WINDOW: 60s. COLLATERAL: zero.", 3, 0, 0f, 60f),
                        M("COLD DELIVERY", "Two handlers meeting at the quay. End the exchange.", 4, 1, 1.4f, 60f),
                        M("MARKET DRIFT", "Runners on the move through the stalls. Lead your shots.", 5, 2, 1.7f, 60f),
                        M("HARBOUR MASTER", "The ring's fixer walks the pier with his crew.", 6, 3, 1.9f, 65f),
                        M("LOW TIDE", "Full cell scattering at dusk. Clear the waterfront.", 7, 4, 2.2f, 70f),
                        M("LAST BOAT OUT", "They're running for the ferry. Nobody leaves.", 8, 5, 2.5f, 75f),
                    },
                },
                new RegionDef
                {
                    name = "NEON DISTRICT",
                    tagline = "Rain-slick towers, rooftop patrols, and a syndicate that never sleeps.",
                    missions = new[]
                    {
                        M("SIGNAL INTRUSION", "A lone spotter on the comms tower. Silence him.", 4, 1, 1.6f, 55f),
                        M("WET STREETS", "Couriers weaving between the arcades. Stay patient.", 5, 2, 2.0f, 60f),
                        M("PENTHOUSE PROBLEM", "The syndicate's accountant and his detail. Books close tonight.", 6, 3, 2.2f, 60f),
                        M("BLACKOUT", "Guards sweeping the plaza in the dark. Time it right.", 7, 4, 2.5f, 65f),
                        M("SKYLINE HUNT", "Fast movers across the rooftops. No easy shots.", 8, 6, 2.8f, 70f),
                        M("KINGPIN", "The head of the syndicate, exposed for sixty seconds.", 8, 6, 3.0f, 75f),
                    },
                },
            };
        }

        static MissionDef M(string name, string intel, int targets, int movers, float moverSpeed, float timeLimit)
        {
            return new MissionDef
            {
                name = name,
                intel = intel,
                targets = targets,
                movers = movers,
                moverSpeed = moverSpeed,
                timeLimit = timeLimit,
            };
        }
    }
}
