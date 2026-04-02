using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Flyweight
{
    // =========================================================
    // FLYWEIGHT (Intrinsic State)
    // =========================================================
    // Această clasă stochează datele GROI și COMUNE ale aeronavelor.
    // În loc să creăm sute de mii de obiecte cu modelele 3D pe mapă,
    // ele vor fi partajate. Memoria ocupată va fi minimală.
    public class AircraftModelData
    {
        public string ModelName { get; private set; }
        public string TextureImage { get; private set; } // Ocupă teoretic multă memorie (MBs)
        public byte[] Mesh3D { get; private set; } // Ocupă zeci de MBs

        public AircraftModelData(string modelName)
        {
            ModelName = modelName;
            TextureImage = $"{modelName.Replace(" ", "_").ToLower()}_texture.png";
            Mesh3D = new byte[1024]; // Simulăm un fișier mare 3D încărcat o singură dată
            
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"[Flyweight] S-a creat modelul greu {ModelName} în memorie (Randare grafică).");
        }

        // Metodă pentru a arăta randarea (procesează state-ul EXTRINSIC venit de la client)
        public void RenderOnRadar(string flightNumber, double latitude, double longitude)
        {
            // Partea intrinsecă e folosită cu partea locală (extrinsecă)
            // Console.WriteLine($"Rendering {flightNumber} ({ModelName}) at [{latitude}, {longitude}]");
        }
    }

    // =========================================================
    // FLYWEIGHT FACTORY
    // =========================================================
    // Fabrică centrală care construiește modelele, dar cel mai 
    // important: LE RECICLEAZĂ dacă au fost deja create (Caching).
    public class AircraftModelFactory
    {
        private Dictionary<string, AircraftModelData> _models = new Dictionary<string, AircraftModelData>();

        public AircraftModelData GetAircraftModel(string modelName)
        {
            // Dacă textura și mesh-ul 3D pentru "Boeing 737" există deja,
            // NU le mai alocăm din nou memorie. Returnăm pointer-ul către prima cerere!
            if (!_models.ContainsKey(modelName))
            {
                _models[modelName] = new AircraftModelData(modelName);
            }
            return _models[modelName];
        }

        public int GetCacheSize() => _models.Count;
    }

    // =========================================================
    // CONTEXT (Extrinsic State)
    // =========================================================
    // Clasa ce reprezintă „punctul pe radar”. E extrem de ușoară în RAM,
    // conține doar coordonatele și o referință către Flyweight-ul masiv.
    public class RadarBlip
    {
        public string FlightNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Referință partajată
        private AircraftModelData _model;

        public RadarBlip(string flightNum, double lat, double lon, AircraftModelData model)
        {
            FlightNumber = flightNum;
            Latitude = lat;
            Longitude = lon;
            _model = model;
        }

        public void Draw()
        {
            // Pasează coordonatele proprii către flyweight-ul comun pentru randare
            _model.RenderOnRadar(FlightNumber, Latitude, Longitude);
        }

        public string GetSharedModelName() => _model.ModelName;
    }
}
