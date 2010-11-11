﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioLink.Data.Model {

    public class Favorite : BioLinkDataObject {
        public string Username { get; set; }
        public int FavoriteID { get; set; }
        public int FavoriteParentID { get; set; }
        public bool IsGroup { get; set; }
        public string GroupName { get; set; }
        public int NumChildren { get; set; }
        public bool IsGlobal { get; set; }
        public FavoriteType FavoriteType { get; set; }
        public int ID1 { get; set; }
        public string ID2 { get; set; }
    }

    public class TaxonFavorite : Favorite {
        public int TaxaID { get; set; }
        public int TaxaParentID { get; set; }
        public string Epithet { get; set; }
        public string TaxaFullName { get; set; }
        public string YearOfPub { get; set; }
        public string KingdomCode { get; set; }
        public string ElemType { get; set; }
        public bool Unverified { get; set; }
        public bool Unplaced { get; set; }
        public int Order { get; set; }
        public string Rank { get; set; }
        public bool ChgComb { get; set; }
        public string NameStatus { get; set; }        
    }

    public class SiteFavorite : Favorite {
        public int ElemID { get; set; }
        public string Name { get; set; }
        public string ElemType { get; set; }        
    }

    public class ReferenceFavorite : Favorite {
        public int RefID { get; set; }
        public string RefCode { get; set; }
        public string FullRTF { get; set; }        
    }

    public class DistRegionFavorite : Favorite {
        public int DistRegionID { get; set; }
        public int DistRegionParentID { get; set; }
        public string DistRegionName { get; set; }        
    }

    public class BiotaStorageFavorite : Favorite {
        public int BiotaStorageID { get; set; }
        public int BiotaStorageParentID { get; set; }
        public string BiotaStorageName { get; set; }
    }

    public enum FavoriteType {
        Taxa,
        Site,
//        Character,  // No morphology at this stage...
        Reference,
        DistRegion,
        BiotaStorage
    }
}
