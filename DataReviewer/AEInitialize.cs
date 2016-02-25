using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS;

namespace DataReviewer
{
    class AEInitializer
    {
        public static void Initialize() 
        {
            RuntimeManager.Bind(ProductCode.EngineOrDesktop);
            IAoInitialize iao = new AoInitialize();
            iao.Initialize(esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB);
            

        }

        public static void Initailize3D() 
        {
            RuntimeManager.Bind(ProductCode.EngineOrDesktop);
            IAoInitialize iao = new AoInitialize();
            iao.Initialize(esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB);

            if((iao.IsExtensionCodeAvailable(esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB,esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst) == esriLicenseStatus.esriLicenseAvailable)&&(iao.IsExtensionCheckedOut(esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst)== true)  )
                iao.CheckOutExtension(esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst);
        }
    }
}
