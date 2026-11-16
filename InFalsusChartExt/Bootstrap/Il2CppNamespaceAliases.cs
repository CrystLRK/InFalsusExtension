#if BEPINEX
extern alias GameData;
global using Il2Cppifapp = ifapp;
global using Il2Cpp_J = _J;
global using Il2Cpp_k = _k;
global using Il2Cpp_n = _n;
global using Il2Cpp_b = _b;
global using Il2Cpp_E = _E;
global using Il2Cpp_K = GameData::_K;
global using Il2CppStr = Str;
global using Il2CppFastText = FastText;
global using Il2CppFMOD = FMOD;
#endif

#if MELONLOADER
global using IFCEFlickNoteV3 = Il2Cpp.FlickNoteV3;
global using IFCEGenericGameplayBackground =
    Il2Cpp.GenericGameplayBackground;
global using IFCEJudgementLineMeshEffect =
    Il2Cpp.JudgementLineMeshEffect;
#elif BEPINEX
global using IFCEFlickNoteV3 = global::FlickNoteV3;
global using IFCEGenericGameplayBackground =
    global::GenericGameplayBackground;
global using IFCEJudgementLineMeshEffect =
    global::JudgementLineMeshEffect;
#endif
