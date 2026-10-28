using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class PaintedEffectSlicingTests
    {
        [Test]
        public void OffsetEffectFramesKeepTheirWholeArtworkAndExcludeNeighbours()
        {
            string png = AssetDatabase.GenerateUniqueAssetPath("Assets/OrpheusBelow/Tests/Editor/FxSlicingFixture.png");
            string asset = AssetDatabase.GenerateUniqueAssetPath("Assets/OrpheusBelow/Tests/Editor/FxSlicingFixture.asset");
            var texture = new Texture2D(400,400,TextureFormat.RGBA32,false);
            var pixels = new Color32[400*400]; var boxes = new RectInt[16];
            int[] centersX = { 60,160,275,365 }, centersY = { 85,190,265,350 }, heights = { 110,50,50,40 };
            try
            {
                for(int row=0;row<4;row++) for(int col=0;col<4;col++)
                {
                    int index=row*4+col, yTop=centersY[row]+(row==0 ? col*3 : 0)-heights[row]/2;
                    boxes[index]=new RectInt(centersX[col]-30,400-yTop-heights[row],60,heights[row]);
                    var b=boxes[index]; var color=new Color32((byte)(index*11+10),80,120,255);
                    for(int y=b.yMin;y<b.yMax;y++) for(int x=b.xMin;x<b.xMax;x++) pixels[y*400+x]=color;
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(png,texture.EncodeToPNG());
                AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
                var library=ScriptableObject.CreateInstance<EnemyArtLibrary>(); AssetDatabase.CreateAsset(library,asset);
                Type builder=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OrpheusBelow.EditorTools.PaintedBestiaryAssetBuilder")).First(t=>t!=null);
                var sprites=(Sprite[])builder.GetMethod("Bake",BindingFlags.Static|BindingFlags.NonPublic)
                    .Invoke(null,new object[]{png,4,4,null,library,true});
                Assert.That(sprites.Length,Is.EqualTo(16));
                for(int i=0;i<16;i++)
                {
                    var rect=sprites[i].rect; var b=boxes[i];
                    Assert.That(rect.xMin,Is.LessThanOrEqualTo(b.xMin),"Left edge clipped in frame "+i);
                    Assert.That(rect.yMin,Is.LessThanOrEqualTo(b.yMin),"Bottom edge clipped in frame "+i);
                    Assert.That(rect.xMax,Is.GreaterThanOrEqualTo(b.xMax),"Right edge clipped in frame "+i);
                    Assert.That(rect.yMax,Is.GreaterThanOrEqualTo(b.yMax),"Top edge clipped in frame "+i);
                    for(int y=(int)rect.yMin;y<(int)rect.yMax;y++) for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                        if(pixels[y*400+x].a>0) Assert.That(pixels[y*400+x].r,Is.EqualTo((byte)(i*11+10)),"Neighbour leaked into frame "+i);
                }
            }
            finally
            { Object.DestroyImmediate(texture); AssetDatabase.DeleteAsset(png); AssetDatabase.DeleteAsset(asset); }
        }
    }
}
