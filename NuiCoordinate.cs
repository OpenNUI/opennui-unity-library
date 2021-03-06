﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNUI.Unity.Library
{
    public static class NuiCoordinate
    {
        public static Vec2 JointPostoDepthPos(Vec3 JointPos, DepthInfo depthInfo)
        {
            if (depthInfo.EnableCoordinate == false)
                return new Vec2(0, 0);

            double depthX = (JointPos.x / JointPos.z - depthInfo.JointDepthXFix) * depthInfo.JointDepthXMult;
            double depthY = (JointPos.y / JointPos.z - depthInfo.JointDepthYFix) * depthInfo.JointDepthYMult;

            if (depthX > 1) depthX = 1;
            else if (depthX < -1) depthX = -1;

            if (depthY > 1) depthY = 1;
            else if (depthY < -1) depthY = -1;

            depthX += 1;
            depthY += 1;
            return new Vec2((int)(depthX * depthInfo.Width / 2), (int)(depthInfo.Height - (depthY * depthInfo.Width / 2)));
        }

        public static Vec3 DepthPostoJointPos(Vec2 depthPos, DepthData depth)
        {
            if (depth.Description.EnableCoordinate == false)
                return new Vec3(0, 0, 0);

            if (depth == null || depth.FrameData == null || depth.FrameData.Length <= 0)
                return new Vec3(0, 0, 0);

            if (depthPos.x >= depth.Description.Width) depthPos.x = depth.Description.Width - 1;
            else if (depthPos.x < 0) depthPos.x = 0;
            if (depthPos.y >= depth.Description.Height) depthPos.y = depth.Description.Height - 1;
            else if (depthPos.y < 0) depthPos.y = 0;

            if (((int)depthPos.x + (int)depthPos.y * depth.Description.Width) > depth.FrameData.Length)
                return new Vec3(0, 0, 0);

            double depthValue = depth.FrameData[(int)depthPos.x + (int)depthPos.y * depth.Description.Width];

            double depthWidth = (double)depth.Description.Width;
            double depthHeight = (double)depth.Description.Height;

            Vec3 vec = new Vec3(0, 0, 0);

            double jZ = (double)depthValue * depth.Description.DepthToJointZMult;

            vec.x = ((depthPos.x * (2 / depthWidth)) - 1) / depth.Description.JointDepthXMult + depth.Description.JointDepthXFix;

            vec.y = (-(depthPos.y - depthHeight) * 2 / depthWidth - 1) / depth.Description.JointDepthYMult + depth.Description.JointDepthYFix;

            vec.x *= jZ;
            vec.y *= jZ;
            vec.z = jZ;

            return vec;
        }

    }
}
