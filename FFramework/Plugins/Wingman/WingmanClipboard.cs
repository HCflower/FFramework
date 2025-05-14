#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace WingmanInspector {

    public class WingmanClipboard {

        public readonly List<WingmanComponentCopy> Copies = new();
        
        public void CopyComponents(List<Component> components) {
            Copies.Clear();
            foreach (Component component in components) {
                Copies.Add(new(component));
            }
        }

    }
    
}
#endif