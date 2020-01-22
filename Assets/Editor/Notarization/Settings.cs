using UnityEngine;

namespace Notarization {

    public class Settings : ScriptableObject {

        public string user;
        public string certId;
        public string bundleId;
        public string file;
        public bool autoNotarizeOnOSXBuild;
        public bool blockUntilFinished = true;
        public bool mono = true;
        public bool steamOverlay = false;
    }
}
