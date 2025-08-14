using UnityEngine;

namespace Spider_Man.Bosses.A_Start_Search_Algorithm.Runtime.DrawerDir
{
    public class LineDrawer : Drawer
    {
        public LineDrawer()
        {
			
        }
		
        public override int Draw(ref Vector3[] buffer, params object[] args)
        {
            buffer[0] = (Vector3)args[0];
            buffer[1] = (Vector3)args[1];
            return 2;
        }
    }
}