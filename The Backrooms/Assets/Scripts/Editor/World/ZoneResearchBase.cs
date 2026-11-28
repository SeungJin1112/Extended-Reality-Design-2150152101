using System.Collections.Generic;
using UnityEngine;
using Faces = Backrooms.EditorTools.BackroomsMeshFactory.Faces;

namespace Backrooms.EditorTools.World
{
    public static class ZoneResearchBase
    {
        const string S = "Assets/Structure/ResearchBase";
        const float W = 36f, D = 34f, H = 2.9f;
        const float CorX = 12f, CorW = 3f, CorD = 30f;
        const float SideZ = 26f, SideW = 3f, SideEnd = 25f;
        const float HoleZ = 27.5f, HoleW = 1.5f, HoleTop = 2.2f;
        const float ShellX = 25.25f, ShellZ = 21f, ShellW = 9f, ShellD = 13f, ShellH = 2.75f;

        public static ZoneKit Build(string name, Vector3 origin, bool abandoned, Level0Look level0)
        {
            var k = new ZoneKit(name, S, origin, W, D, H, abandoned ? 404 : 100) { WallThickness = 0.25f };
            k.Room(CorX, 0, CorW, CorD);
            k.Room(5f, 9f, CorX - 5f, 8f);
            k.Room(CorX + CorW, 3f, 6f, 7f);
            k.Room(CorX + CorW, SideZ, SideEnd - CorX - CorW, SideW);
            k.Partition(CorX, 9f, CorX, 17f, (4f, 0.95f));
            k.Partition(CorX + CorW, 3f, CorX + CorW, 10f, (3.5f, 0.95f));
            k.Opening(CorX + CorW / 2, 0f, 1.08f);
            k.Opening(SideEnd, HoleZ, HoleW, HoleTop);

            var bricks = "Assets/Structure/AmusementPark/Textures";
            var tex = $"{S}/Textures";
            var floor = WorldAssets.SetMaterial(S, "BasementFloorMaterial", tex, "Concrete048", 2.5f, new Color(0.62f, 0.63f, 0.62f), 0.5f);
            var wall = WorldAssets.SetMaterial(S, "BasementWallMaterial", "Assets/Structure/Common/Textures", "Plaster002", 2f, new Color(0.84f, 0.84f, 0.8f), 0.2f, 0.7f);
            var lower = WorldAssets.SetMaterial(S, "BasementWallLowerMaterial", "Assets/Structure/Common/Textures", "Plaster002", 2f, new Color(0.46f, 0.48f, 0.45f), 0.45f, 0.7f);
            var ceiling = WorldAssets.SetMaterial(S, "BasementCeilingMaterial", tex, "Concrete034", 3f, new Color(0.62f, 0.63f, 0.62f), 0.1f);
            var trim = WorldAssets.Lit(S, "BasementTrimMaterial", new Color(0.16f, 0.17f, 0.17f), smoothness: 0.4f);
            k.Build(new ZoneKit.Surfaces { Floor = floor, Wall = wall, WallLower = lower, WainscotHeight = 1.1f, Ceiling = ceiling, Baseboard = trim, BaseboardHeight = 0.1f });

            BuildCeilingServices(k);
            BuildLights(k, abandoned);
            BuildSecurityRoom(k, abandoned);
            BuildStorage(k, abandoned);
            BuildBreach(k, wall, lower, abandoned);
            BuildShell(k, name, level0);
            BuildCorridorDressing(k, abandoned);
            BuildSigns(k, abandoned);
            if (!abandoned)
            {
                ResearcherPlacement.Place(k.Root, k.Props);
                ResearchBaseNarration.Place(k.Root);
            }

            var entry = k.Place("DoorMetal", CorX + CorW / 2, -0.12f, 0f, isStatic: false);
            if (entry != null) entry.name = "EntryDoor";
            var sec = k.Place("DoorOffice", CorX, 13f, 90f, isStatic: false);
            if (sec != null) sec.name = "SecurityDoor";
            var sto = k.Place("DoorMetal", CorX + CorW, 6.5f, 90f, isStatic: false);
            if (sto != null) sto.name = "StorageDoor";

            k.Marker("EntryPoint", CorX + CorW / 2, 1.3f, 0f);
            k.Marker("PreviewBreach", 21.8f, HoleZ, 90f);
            k.Marker("PreviewSecurity", 10.8f, 12.5f, 250f);
            k.Reserve(CorX, 0f, CorW, 3f);
            k.LightProbes(2.5f, 0.5f, 1.7f);
            k.ReflectionProbe(CorX + CorW / 2, 1.5f, CorD / 2, new Vector3(CorW, H, CorD));
            return k;
        }

        static void BuildCeilingServices(ZoneKit k)
        {
            var grey = WorldAssets.Lit(S, "PipeGreyMaterial", new Color(0.55f, 0.57f, 0.58f), smoothness: 0.5f, metallic: 0.5f);
            var red = WorldAssets.Lit(S, "PipeRedMaterial", new Color(0.55f, 0.1f, 0.08f), smoothness: 0.5f, metallic: 0.3f);
            var green = WorldAssets.Lit(S, "PipeGreenMaterial", new Color(0.2f, 0.34f, 0.26f), smoothness: 0.45f, metallic: 0.3f);
            var galv = WorldAssets.Lit(S, "DuctMaterial", new Color(0.66f, 0.68f, 0.7f), smoothness: 0.6f, metallic: 0.8f);
            var services = k.Child("CeilingServices", k.Details);
            var pipes = new (float x, float y, float r, Material m)[] { (CorX + CorW - 0.25f, H - 0.18f, 0.07f, grey), (CorX + CorW - 0.5f, H - 0.14f, 0.045f, red), (CorX + CorW - 0.7f, H - 0.2f, 0.055f, green) };
            foreach (var p in pipes)
            {
                k.Primitive("Pipe", "Cylinder.fbx", new Vector3(p.x, p.y, CorD / 2), new Vector3(90, 0, 0), new Vector3(p.r * 2, CorD / 2, p.r * 2), p.m, services);
                for (float z = 1.5f; z < CorD; z += 3f)
                    k.Box("Hanger", new Vector3(p.x, (p.y + H) / 2, z), new Vector3(0.02f, H - p.y, 0.02f), grey, services);
            }
            k.Primitive("SidePipe", "Cylinder.fbx", new Vector3((CorX + SideEnd) / 2, H - 0.18f, SideZ + SideW - 0.25f), new Vector3(0, 0, 90), new Vector3(0.14f, (SideEnd - CorX) / 2, 0.14f), grey, services);
            k.Box("CableTray", new Vector3(CorX + 0.35f, H - 0.12f, CorD / 2), new Vector3(0.4f, 0.06f, CorD - 0.4f), galv, services);
            for (int i = 0; i < 4; i++)
                k.Box("Cable", new Vector3(CorX + 0.22f + i * 0.08f, H - 0.08f, CorD / 2), new Vector3(0.03f, 0.03f, CorD - 0.6f), WorldAssets.Lit(S, "CableMaterial", new Color(0.04f, 0.04f, 0.045f), smoothness: 0.4f), services);
            k.Box("Duct", new Vector3((CorX + CorW + SideEnd) / 2, H - 0.25f, SideZ + 0.5f), new Vector3(SideEnd - CorX - CorW, 0.35f, 0.6f), galv, services);
            WorldAssets.UseProbes(services.gameObject);
        }

        static void Fixture(ZoneKit k, Transform parent, float x, float z, float y, bool dead, float intensity)
        {
            var housing = WorldAssets.Lit(S, "TubeHousingMaterial", new Color(0.8f, 0.81f, 0.78f), smoothness: 0.5f, metallic: 0.3f);
            var tube = WorldAssets.Unlit(S, "TubeLightMaterial", new Color(0.93f, 0.97f, 1f));
            var off = WorldAssets.Lit(S, "TubeOffMaterial", new Color(0.55f, 0.56f, 0.55f), smoothness: 0.6f);
            k.Box("TubeHousing", new Vector3(x, y - 0.04f, z), new Vector3(0.22f, 0.07f, 1.28f), housing, parent);
            k.Quad("Tube", new Vector3(x, y - 0.077f, z), new Vector2(0.14f, 1.22f), new Vector3(-90, 0, 0), dead ? off : tube, parent);
            if (!dead) k.AreaLight(x, y - 0.09f, z, 0.25f, 1.25f, new Color(0.9f, 0.96f, 1f), intensity);
        }

        static void BuildLights(ZoneKit k, bool abandoned)
        {
            var fixtures = k.Child("Fixtures", k.Details);
            int n = 0;
            for (float z = 2f; z < CorD; z += 3.4f, n++)
            {
                bool dead = n == 4 || (abandoned && k.Rng.NextDouble() < 0.6);
                Fixture(k, fixtures, CorX + CorW / 2 - 0.2f, z, H, dead, abandoned ? 18f : 30f);
            }
            Fixture(k, fixtures, 19f, SideZ + SideW / 2, H, abandoned, 26f);
            foreach (var (px, pz, dead) in new[] { (7.2f, 11.2f, false), (9.8f, 11.2f, abandoned), (7.2f, 14.8f, abandoned), (9.8f, 14.8f, false) })
            {
                k.Box("PanelFrame", new Vector3(px, H - 0.015f, pz), new Vector3(0.66f, 0.03f, 1.26f), WorldAssets.Lit(S, "PanelFrameMaterial", new Color(0.82f, 0.82f, 0.8f), smoothness: 0.4f), fixtures);
                k.Quad("Panel", new Vector3(px, H - 0.032f, pz), new Vector2(0.6f, 1.2f), new Vector3(-90, 0, 0), dead ? WorldAssets.Lit(S, "TubeOffMaterial", new Color(0.55f, 0.56f, 0.55f), smoothness: 0.6f) : WorldAssets.Unlit(S, "PanelLightMaterial", new Color(0.95f, 0.98f, 1f)), fixtures);
                if (!dead) k.AreaLight(px, H - 0.05f, pz, 0.6f, 1.2f, new Color(0.92f, 0.96f, 1f), abandoned ? 5f : 9f);
            }
            k.Place("CagedHangingLight", 18.2f, 6.5f, 0f, y: H - 0.6f, snap: false, isStatic: true);
            if (!abandoned) k.PointLight(18.2f, H - 0.75f, 6.5f, new Color(1f, 0.85f, 0.65f), 1.6f, 6f);
        }

        static void BuildSecurityRoom(ZoneKit k, bool abandoned)
        {
            var room = k.Child("SecurityRoom", k.Props);
            foreach (var z in new[] { 10.5f, 13.5f })
            {
                var desk = k.Place("MetalOfficeDesk", 6f, z + 1f, 90f, parent: room, isStatic: true);
                if (desk == null) continue;
                float top = ZoneKit.Bounds(desk).max.y - k.Root.position.y;
                if (abandoned && z > 12f) continue;
                k.Place("Television02", 5.8f, z + 0.6f, 90f, y: top, parent: room);
                k.Place("ClassicLaptop", 6.1f, z + 1.4f, 100f, y: top, parent: room);
                k.Place("OfficeNotepads", 6.3f, z + 0.2f, 20f, y: top, parent: room);
                if (!abandoned) k.Place("DeskLampArm01", 5.6f, z + 1.8f, 120f, y: top, parent: room);
                k.Place(z < 12f ? "Clipboard" : "VintageRadioTransceiver", 6.4f, z + 1.7f, 60f, y: top, parent: room);
                k.Place("MetalStool02", 7.1f, z + 1f, 0f, parent: room);
            }
            if (!abandoned) k.Place("OldGasMask", 10.8f, 16.4f, 200f, y: 0.0f, parent: room);
            k.Place("SteelFrameShelves01", 11.4f, 10f, 270f, parent: room);
            k.Place("WallClock", 8.5f, 16.97f, 180f, y: 2.1f, snap: false, parent: room, isStatic: true);

            var frame = WorldAssets.Lit(S, "MonitorFrameMaterial", new Color(0.08f, 0.08f, 0.09f), smoothness: 0.5f);
            var off = WorldAssets.Lit(S, "MonitorOffMaterial", new Color(0.03f, 0.04f, 0.04f), smoothness: 0.9f);
            for (int i = 0; i < 4; i++)
            {
                float x = 6.3f + i * 1.05f, y = 1.75f;
                k.Box("MonitorFrame", new Vector3(x, y, 16.95f), new Vector3(0.95f, 0.6f, 0.06f), frame, room);
                var feed = WorldAssets.Unlit(S, $"CameraFeed{i}Material", new Color(0.92f, 0.95f, 0.9f), ProceduralTextures.CameraFeed($"{S}/Textures/Generated/CameraFeed{i}BaseColor.png", i + 1));
                k.Quad("Screen", new Vector3(x, y, 16.915f), new Vector2(0.86f, 0.52f), new Vector3(0, 0, 0), abandoned && i != 2 ? off : feed, room);
            }
            if (!abandoned) k.PointLight(7.8f, 1.6f, 16.2f, new Color(0.85f, 0.9f, 0.8f), 0.6f, 3.5f, room);

            var rack = WorldAssets.Lit(S, "ServerRackMaterial", new Color(0.07f, 0.075f, 0.08f), smoothness: 0.55f, metallic: 0.6f);
            var led = WorldAssets.Unlit(S, "LedGreenMaterial", new Color(0.3f, 1f, 0.45f));
            foreach (var z in new[] { 9.6f, 10.3f })
            {
                k.Box("ServerRack", new Vector3(5.45f, 1.0f, z), new Vector3(0.85f, 2.0f, 0.64f), rack, room, collider: true);
                if (abandoned) continue;
                for (int u = 0; u < 10; u++)
                    k.Box("Led", new Vector3(5.885f, 0.4f + u * 0.14f, z - 0.2f + (u % 3) * 0.06f), new Vector3(0.01f, 0.015f, 0.015f), led, room);
            }
            k.Reserve(5f, 9f, 7f, 8f);
        }

        static void BuildStorage(ZoneKit k, bool abandoned)
        {
            k.Place("SteelFrameShelves02", 20.6f, 4f, 270f);
            k.Place("WornMetalRack", 20.6f, 7f, 270f);
            var area = new Rect(16f, 3.5f, 4f, 6f);
            var items = new List<string> { "PlasticCrate01", "PlasticCrate02", "CardboardBox01", "CardboardBox01", "Barrel02", "CementBag", "MetalToolbox", "PlasticCrate03" };
            if (!abandoned) items.AddRange(new[] { "PropaneTank", "PortableGenerator", "IndustrialPlasticContainer" });
            foreach (var p in items) k.Scatter(p, area, 0.45f);
        }

        static void BuildBreach(ZoneKit k, Material wall, Material lower, bool abandoned)
        {
            var chunks = new BackroomsMeshFactory();
            var chunksLow = new BackroomsMeshFactory();
            float x0 = SideEnd, t = k.WallThickness, z0 = HoleZ - HoleW / 2, z1 = HoleZ + HoleW / 2;
            for (int i = 0; i < 7; i++)
            {
                float z = Mathf.Lerp(z0 + 0.08f, z1 - 0.08f, i / 6f);
                float drop = 0.05f + (float)k.Rng.NextDouble() * 0.28f, wdt = 0.12f + (float)k.Rng.NextDouble() * 0.18f;
                chunks.AddBox(new Vector3(x0 + t / 2, HoleTop - drop / 2, z), new Vector3(t * (0.5f + (float)k.Rng.NextDouble() * 0.5f), drop, wdt), Faces.All);
            }
            foreach (var side in new[] { -1f, 1f })
                for (int i = 0; i < 9; i++)
                {
                    float y = 0.08f + i * 0.24f + (float)k.Rng.NextDouble() * 0.1f;
                    float jut = 0.04f + (float)k.Rng.NextDouble() * 0.22f, h = 0.12f + (float)k.Rng.NextDouble() * 0.2f;
                    float z = side < 0 ? z0 + jut / 2 : z1 - jut / 2;
                    var target = y < 1.1f ? chunksLow : chunks;
                    target.AddBox(new Vector3(x0 + t / 2, y, z), new Vector3(t * (0.55f + (float)k.Rng.NextDouble() * 0.45f), h, jut), Faces.All);
                }
            k.MeshObject("BreachEdges", chunks, wall, 3f);
            k.MeshObject("BreachEdgesLower", chunksLow, lower, 3f);

            var rebar = WorldAssets.Lit(S, "RebarMaterial", new Color(0.32f, 0.2f, 0.12f), smoothness: 0.3f, metallic: 0.6f);
            var debris = k.Child("Debris", k.Details);
            for (int i = 0; i < 3; i++)
                k.Primitive("Rebar", "Cylinder.fbx", new Vector3(x0 + 0.1f, 0.6f + i * 0.6f, z0 - 0.05f + (float)k.Rng.NextDouble() * 0.1f), new Vector3(0, 0, 90 + (float)(k.Rng.NextDouble() * 30 - 15)), new Vector3(0.012f, 0.18f, 0.012f), rebar, debris);
            var rubble = WorldAssets.Lit(S, "RubbleMaterial", new Color(0.62f, 0.62f, 0.6f), smoothness: 0.1f);
            for (int i = 0; i < 26; i++)
            {
                float s = 0.03f + (float)k.Rng.NextDouble() * 0.12f;
                float x = x0 - 0.2f - (float)k.Rng.NextDouble() * 1.4f + (i % 4 == 0 ? 0.9f : 0f), z = HoleZ + ((float)k.Rng.NextDouble() - 0.5f) * 2.2f;
                var c = k.Box("Rubble", new Vector3(x, s / 2, z), new Vector3(s * 1.4f, s, s), i % 3 == 0 ? wall : rubble, debris);
                c.transform.localRotation = Quaternion.Euler(0, (float)k.Rng.NextDouble() * 360f, (float)k.Rng.NextDouble() * 20f);
            }
            var dust = WorldAssets.Transparent(S, "DustMaterial", new Color(0.55f, 0.55f, 0.52f, 0.6f), ProceduralTextures.WaterStain($"{S}/Textures/Generated/DustBaseColor.png", 9));
            k.Quad("DustPile", new Vector3(x0 - 0.7f, 0.004f, HoleZ), new Vector2(1.8f, 1.6f), new Vector3(90, 30, 0), dust, debris);

            var tapeMat = WorldAssets.Unlit(S, "BarrierTapeMaterial", Color.white, ProceduralTextures.BarrierTape($"{S}/Textures/Generated/BarrierTapeBaseColor.png"));
            var tape = k.Child("BarrierTape", k.Details);
            var strips = abandoned
                ? new[] { (new Vector3(x0 - 0.05f, 1.7f, z0 - 0.2f), new Vector3(x0 - 0.05f, 0.9f, HoleZ + 0.1f)) }
                : new[] { (new Vector3(x0 - 0.05f, 1.9f, z0 - 0.25f), new Vector3(x0 - 0.05f, 0.5f, z1 + 0.25f)), (new Vector3(x0 - 0.05f, 0.55f, z0 - 0.25f), new Vector3(x0 - 0.05f, 1.85f, z1 + 0.25f)), (new Vector3(x0 - 0.05f, 1.2f, z0 - 0.3f), new Vector3(x0 - 0.05f, 1.25f, z1 + 0.3f)) };
            foreach (var (a, b) in strips)
            {
                var s = k.Box("Tape", (a + b) / 2, new Vector3(0.004f, 0.05f, Vector3.Distance(a, b)), tapeMat, tape, isStatic: false);
                s.transform.localRotation = Quaternion.LookRotation(b - a);
            }

            var sheet = WorldAssets.Transparent(S, "PlasticCurtainMaterial", Color.white, ProceduralTextures.PlasticSheet($"{S}/Textures/Generated/PlasticSheetBaseColor.png"), 0.6f);
            k.Quad("TornSheet", new Vector3(x0 - 0.12f, 1.25f, z1 - 0.1f), new Vector2(0.7f, 2.0f), new Vector3(0, 74, 3), sheet, tape);
            k.Quad("TornSheetBack", new Vector3(x0 - 0.12f, 1.25f, z1 - 0.1f), new Vector2(0.7f, 2.0f), new Vector3(0, 254, -3), sheet, tape);

            if (abandoned) return;
            k.Place("FloodLights", 22.6f, HoleZ - 0.8f, 75f, isStatic: true);
            k.SpotLight(22.6f, 1.7f, HoleZ - 0.8f, new Vector3(8f, 75f, 0f), new Color(0.92f, 0.96f, 1f), 4f, 7f, 55f);
            k.Place("IndustrialStorageCart", 21.5f, HoleZ + 1.1f, 0f);
            k.Place("VintageVideoCamera", 21.5f, HoleZ + 1.0f, 90f, y: 0.92f);
            k.Place("Clipboard", 21.2f, HoleZ + 1.25f, 140f, y: 0.92f);
            k.Place("PlasticCrate02", 23.2f, HoleZ + 1.15f, 15f);
            k.Place("MetalDetector", 19.6f, HoleZ + 1.2f, 80f);

            var cable = WorldAssets.Lit(S, "CableMaterial", new Color(0.04f, 0.04f, 0.045f), smoothness: 0.4f);
            var cables = k.Child("FloorCables", k.Details);
            var path = new[] { new Vector3(CorX + 0.25f, 0.02f, 14.4f), new Vector3(CorX + 0.3f, 0.02f, SideZ + 0.6f), new Vector3(20f, 0.02f, SideZ + 0.5f), new Vector3(23.6f, 0.02f, HoleZ - 0.3f), new Vector3(x0 + 0.6f, 0.02f, HoleZ - 0.2f) };
            for (int c = 0; c < 2; c++)
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var a = path[i] + new Vector3(c * 0.06f, 0, c * 0.05f);
                    var b = path[i + 1] + new Vector3(c * 0.06f, 0, c * 0.05f);
                    var seg = k.Box("Cable", (a + b) / 2, new Vector3(0.03f, 0.03f, Vector3.Distance(a, b)), cable, cables);
                    seg.transform.localRotation = Quaternion.LookRotation(b - a);
                }
        }

        static void BuildShell(ZoneKit zone, string zoneName, Level0Look look)
        {
            var shell = new ZoneKit($"{zoneName}Shell", S, zone.Root.position + new Vector3(ShellX, 0, ShellZ), ShellW, ShellD, ShellH, 9) { WallThickness = 0.12f, Thresholds = false };
            shell.Root.SetParent(zone.Architecture, true);
            shell.Root.name = "BackroomsShell";
            shell.Room(0, 0, ShellW, ShellD);
            shell.Opening(0f, HoleZ - ShellZ, HoleW + 0.12f, HoleTop + 0.05f);
            shell.Partition(0f, 4f, 5.5f, 4f, (2.8f, 1.1f));
            shell.Opening(ShellW, 10.5f, 1.08f);
            shell.Build(new ZoneKit.Surfaces { Floor = look.Carpet, Wall = look.Wallpaper, Ceiling = look.Ceiling, Baseboard = look.Baseboard, BaseboardHeight = 0.09f });

            var troffers = shell.Child("Troffers", shell.Details);
            for (float x = 1.5f; x < ShellW; x += 3.6f)
            for (float z = 1.2f; z < ShellD; z += 3.6f)
            {
                if (shell.HitsWall(new Rect(x - 0.35f, z - 0.65f, 0.7f, 1.3f))) continue;
                bool dead = shell.Rng.NextDouble() < 0.15;
                shell.Quad("Troffer", new Vector3(x, ShellH - 0.004f, z), new Vector2(0.6f, 1.2f), new Vector3(-90, 0, 0), dead ? look.TrofferDead : look.Troffer, troffers);
                if (!dead) shell.AreaLight(x, ShellH - 0.02f, z, 0.6f, 1.2f, look.LightColor, look.LightIntensity);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = 0.8f + (float)shell.Rng.NextDouble() * (ShellW - 1.6f), z = 0.8f + (float)shell.Rng.NextDouble() * (ShellD - 1.6f), s = 0.8f + (float)shell.Rng.NextDouble() * 1.4f;
                if (shell.HitsWall(new Rect(x - s / 2, z - s / 2, s, s), 0f)) continue;
                shell.Quad("Stain", new Vector3(x, i % 3 == 0 ? ShellH - 0.008f : 0.006f, z), new Vector2(s, s), new Vector3(i % 3 == 0 ? -90 : 90, i * 47f, 0), look.Stain);
            }
            shell.Place("PlasticBottleGallon", 4f, 9f, 30f);
            var exit = shell.Place("DoorOffice", ShellW + 0.06f, 10.5f, 90f, isStatic: false);
            if (exit != null) exit.name = "ExitDoor";
            zone.Marker("ExitPoint", ShellX + ShellW - 1.2f, ShellZ + 10.5f, 90f);
            shell.LightProbes(2.5f, 0.5f, 1.7f);
        }

        static void BuildCorridorDressing(ZoneKit k, bool abandoned)
        {
            k.Place("KoreanFireExtinguisher01", CorX + CorW - 0.3f, 3.2f, 270f);
            k.Place("FireAlarm", CorX + CorW - 0.02f, 3.6f, 270f, y: 1.45f, snap: false, isStatic: true);
            k.Place("UtilityBox01", CorX + 0.02f, 20.5f, 90f, y: 1.1f, snap: false, isStatic: true);
            k.Place("UtilityBox02", CorX + 0.02f, 22.2f, 90f, y: 1.3f, snap: false, isStatic: true);
            k.Place("SecurityCamera02", CorX + CorW - 0.2f, 0.4f, 200f, y: 2.6f, snap: false, isStatic: true);
            k.Place("SecurityCamera01", CorX + 0.25f, CorD - 0.3f, 140f, y: 2.6f, snap: false, isStatic: true);
            k.Place("MetalTrashCan", CorX + 0.35f, 7.6f, 0f);
            k.Place("HandTruck", CorX + CorW - 0.4f, 18f, 260f);
            k.Place("WetFloorSign01", CorX + 1.2f, 21.5f, 30f);
            if (!abandoned)
            {
                k.Place("CardboardBox01", CorX + CorW - 0.45f, 24.2f, 10f);
                k.Place("PlasticCrate01", CorX + CorW - 0.4f, 23.4f, 80f);
            }
            else
            {
                for (int i = 0; i < 6; i++)
                    k.Place("OfficeNotepads", CorX + 0.5f + (float)k.Rng.NextDouble() * (CorW - 1f), 8f + (float)k.Rng.NextDouble() * 18f, (float)(k.Rng.NextDouble() * 360));
            }
            var stain = WorldAssets.Transparent(S, "FloorStainMaterial", new Color(0.3f, 0.3f, 0.26f, 0.7f), ProceduralTextures.WaterStain($"{S}/Textures/Generated/FloorStainBaseColor.png", 5));
            for (int i = 0; i < 6; i++)
                k.Quad("FloorStain", new Vector3(CorX + 0.6f + (float)k.Rng.NextDouble() * (CorW - 1.2f), 0.004f, 3f + i * 4.3f), new Vector2(1f + (float)k.Rng.NextDouble(), 1f + (float)k.Rng.NextDouble()), new Vector3(90, i * 61f, 0), stain);
        }

        static void BuildSigns(ZoneKit k, bool abandoned)
        {
            var white = WorldAssets.Lit(S, "SignWhiteMaterial", new Color(0.92f, 0.92f, 0.9f), smoothness: 0.3f);
            var red = WorldAssets.Lit(S, "SignRedMaterial", new Color(0.62f, 0.08f, 0.07f), smoothness: 0.3f);
            var yellow = WorldAssets.Lit(S, "SignYellowMaterial", new Color(0.95f, 0.78f, 0.1f), smoothness: 0.3f);
            var signs = k.Child("Signs", k.Details);
            WorldAssets.Sign(signs, S, "LevelSign", new Vector3(CorX + 0.02f, 2.15f, 2.4f), 270f, new Vector2(0.9f, 0.34f), white, "B3  기계실", new Color(0.1f, 0.1f, 0.1f), 0.03f);
            WorldAssets.Sign(signs, S, "StaffOnlySign", new Vector3(CorX + CorW - 0.02f, 1.7f, 1.8f), 90f, new Vector2(0.6f, 0.4f), red, "관계자 외\n출입 금지", Color.white, 0.03f);
            WorldAssets.Sign(signs, S, "SecurityRoomSign", new Vector3(CorX + 0.02f, 2.25f, 14.2f), 270f, new Vector2(0.55f, 0.18f), white, "보안실", new Color(0.1f, 0.1f, 0.1f), 0.03f);
            WorldAssets.Sign(signs, S, "BreachNotice", new Vector3(SideEnd - 0.02f, 1.55f, HoleZ - 1.25f), 90f, new Vector2(0.42f, 0.56f), yellow,
                "경고\n\nSITE-0 관측 구역\n무단 접근 금지\n\n이상 발견 시\n보안실 즉시 보고", new Color(0.08f, 0.08f, 0.08f), 0.02f);
            if (abandoned)
                WorldAssets.Sign(signs, S, "AbandonedNote", new Vector3(6.5f, 1.2f, 16.9f), 180f, new Vector2(0.4f, 0.28f), white, "철수 완료\n누구도 남지 않음", new Color(0.4f, 0.05f, 0.05f), 0.018f);
        }
    }
}
