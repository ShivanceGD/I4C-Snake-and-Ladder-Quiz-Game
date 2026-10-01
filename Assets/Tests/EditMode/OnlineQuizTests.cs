using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class OnlineQuizTests
{
    [Test]
    public void FromScriptableObjectCopiesAllQuestionFields()
    {
        Object source = CreatePack();

        object dto = InvokeStatic("QuizPackOnlineConverter", "FromScriptableObject", source);
        IList questions = GetField<IList>(dto, "questions");
        object question = questions[0];
        object character = GetField<object>(question, "characterData");

        Assert.AreEqual(1, questions.Count);
        Assert.AreEqual("Question?", GetField<string>(question, "question"));
        Assert.AreEqual("B", GetField<string[]>(question, "options")[1]);
        Assert.AreEqual(1, GetField<int>(question, "correctAnswerIndex"));
        Assert.AreEqual("Medium", GetField<object>(question, "questionsDifficulty").ToString());
        Assert.IsTrue(GetField<bool>(question, "isHintAllowed"));
        Assert.AreEqual(20f, GetField<float>(question, "timeLimit"));
        Assert.AreEqual("Guide", GetField<string>(character, "characterName"));
        Assert.AreEqual("Anonymous", GetField<object>(character, "characterGender").ToString());
    }

    [Test]
    public void ToRuntimeScriptableObjectCreatesPlayableQuizPack()
    {
        object dto = InvokeStatic("QuizPackOnlineConverter", "FromScriptableObject", CreatePack());
        SetField(dto, "displayName", "Runtime Pack");

        Object runtimePack = (Object)InvokeStatic("QuizPackOnlineConverter", "ToRuntimeScriptableObject", dto);
        IList questions = GetField<IList>(runtimePack, "questions");
        object question = questions[0];

        Assert.IsNotNull(runtimePack);
        Assert.AreEqual("Runtime Pack", runtimePack.name);
        Assert.AreEqual(1, questions.Count);
        Assert.AreEqual("Question?", GetField<string>(question, "question"));
        Assert.AreEqual("Medium", GetField<object>(question, "questionsDifficulty").ToString());
    }

    [Test]
    public void JsonRoundTripPreservesQuizPack()
    {
        object dto = InvokeStatic("QuizPackOnlineConverter", "FromScriptableObject", CreatePack());

        string json = (string)InvokeStatic("QuizPackOnlineConverter", "ToJson", dto);
        object parsed = InvokeStatic("QuizPackOnlineConverter", "FromJson", json);
        IList sourceQuestions = GetField<IList>(dto, "questions");
        IList parsedQuestions = GetField<IList>(parsed, "questions");

        Assert.IsNotNull(parsed);
        Assert.AreEqual(sourceQuestions.Count, parsedQuestions.Count);
        Assert.AreEqual(
            GetField<int>(sourceQuestions[0], "correctAnswerIndex"),
            GetField<int>(parsedQuestions[0], "correctAnswerIndex"));
    }

    [Test]
    public void ManifestDeserializesEntries()
    {
        string json = "{\"schemaVersion\":\"1.0\",\"updatedAtIso\":\"2026-01-01T00:00:00Z\",\"packs\":[{\"packId\":\"password_safety\",\"displayName\":\"Password Safety\",\"categoryName\":\"Security\",\"isActive\":true,\"questionCount\":3}]}";

        object manifest = JsonUtility.FromJson(json, FindType("QuizPackManifestDTO"));
        IList packs = GetField<IList>(manifest, "packs");

        Assert.IsNotNull(manifest);
        Assert.AreEqual(1, packs.Count);
        Assert.AreEqual("password_safety", GetField<string>(packs[0], "packId"));
        Assert.IsTrue(GetField<bool>(packs[0], "isActive"));
    }

    [Test]
    public void RepositoryGroupsAndSortsByCategory()
    {
        Object config = CreateScriptableObject("OnlineQuizConfigSO");
        object repository = CreateInstance("OnlineQuizRepository", config, null);
        IList entries = CreateList("QuizPackManifestEntryDTO",
            CreateManifestEntry("b", "B", "Fraud", 2),
            CreateManifestEntry("a", "A", "Fraud", 1),
            CreateManifestEntry("c", "C", "Safety", 0));

        object grouped = InvokeInstance(repository, "GroupByCategory", entries);
        IList fraudEntries = (IList)grouped.GetType().GetProperty("Item").GetValue(grouped, new object[] { "Fraud" });

        Assert.AreEqual(2, GetProperty<int>(grouped, "Count"));
        Assert.AreEqual("a", GetField<string>(fraudEntries[0], "packId"));
        Assert.AreEqual("b", GetField<string>(fraudEntries[1], "packId"));
    }

    [Test]
    public void LocalFallbackReturnsAssignedPacks()
    {
        Object config = CreateScriptableObject("OnlineQuizConfigSO");
        SetField(config, "useLocalQuizFallback", true);
        Object pack = CreatePack();
        object fallbackPacks = CreateArray("QuizPackSO", pack);
        object repository = CreateInstance("OnlineQuizRepository", config, fallbackPacks);

        object fallback = InvokeInstance(repository, "GetLocalFallbackPacks");

        Assert.AreEqual(1, GetProperty<int>(fallback, "Count"));
        Assert.AreSame(pack, GetProperty<object>(fallback, "Item", 0));
    }

    [Test]
    public void TournamentDataStoresOnlineQuizMetadata()
    {
        object tournament = CreateInstance("TournamentData");
        SetField(tournament, "quizPackId", "password_safety");
        SetField(tournament, "quizPackDisplayName", "Password Safety");
        SetField(tournament, "quizCategoryName", "Security");
        SetField(tournament, "quizVersion", "1.0");
        SetField(tournament, "usesOnlineQuizPack", true);
        SetField(tournament, "selectedQuizPackName", "Password Safety");

        Assert.IsTrue(GetField<bool>(tournament, "usesOnlineQuizPack"));
        Assert.AreEqual("password_safety", GetField<string>(tournament, "quizPackId"));
        Assert.AreEqual("Password Safety", GetField<string>(tournament, "selectedQuizPackName"));
    }

    private static Object CreatePack()
    {
        Object pack = CreateScriptableObject("QuizPackSO");
        pack.name = "Pack One";

        object question = CreateInstance("QuizQuestionData");
        SetField(question, "question", "Question?");
        SetField(question, "options", new[] { "A", "B", "C", "D" });
        SetField(question, "correctAnswerIndex", 1);
        SetField(question, "questionsDifficulty", ParseEnum("QuestionsDifficulty", "Medium"));
        SetField(question, "isHintAllowed", true);
        SetField(question, "timeLimit", 20f);
        SetField(question, "characterData", CreateCharacterData());

        SetField(pack, "questions", CreateList("QuizQuestionData", question));
        return pack;
    }

    private static object CreateCharacterData()
    {
        object characterData = CreateInstance("CharacterData");
        SetField(characterData, "characterName", "Guide");
        SetField(characterData, "characterInfo", "Info");
        SetField(characterData, "characterGender", ParseEnum("CharacterGender", "Anonymous"));
        return characterData;
    }

    private static object CreateManifestEntry(string packId, string displayName, string categoryName, int sortOrder)
    {
        object entry = CreateInstance("QuizPackManifestEntryDTO");
        SetField(entry, "packId", packId);
        SetField(entry, "displayName", displayName);
        SetField(entry, "categoryName", categoryName);
        SetField(entry, "sortOrder", sortOrder);
        SetField(entry, "isActive", true);
        return entry;
    }

    private static Object CreateScriptableObject(string typeName)
    {
        return ScriptableObject.CreateInstance(FindType(typeName));
    }

    private static object CreateInstance(string typeName, params object[] args)
    {
        return System.Activator.CreateInstance(FindType(typeName), args);
    }

    private static object InvokeStatic(string typeName, string methodName, params object[] args)
    {
        return Invoke(FindType(typeName), null, methodName, args);
    }

    private static object InvokeInstance(object target, string methodName, params object[] args)
    {
        return Invoke(target.GetType(), target, methodName, args);
    }

    private static object Invoke(System.Type type, object target, string methodName, params object[] args)
    {
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        Assert.IsNotNull(method, $"{type.Name}.{methodName} was not found.");
        return method.Invoke(target, args);
    }

    private static IList CreateList(string elementTypeName, params object[] items)
    {
        System.Type listType = typeof(List<>).MakeGenericType(FindType(elementTypeName));
        IList list = (IList)System.Activator.CreateInstance(listType);

        foreach (object item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static object CreateArray(string elementTypeName, params object[] items)
    {
        System.Array array = System.Array.CreateInstance(FindType(elementTypeName), items.Length);

        for (int i = 0; i < items.Length; i++)
        {
            array.SetValue(items[i], i);
        }

        return array;
    }

    private static object ParseEnum(string typeName, string value)
    {
        return System.Enum.Parse(FindType(typeName), value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} was not found.");
        return (T)field.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} was not found.");
        field.SetValue(target, value);
    }

    private static T GetProperty<T>(object target, string propertyName, params object[] index)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(property, $"{target.GetType().Name}.{propertyName} was not found.");
        return (T)property.GetValue(target, index);
    }

    private static System.Type FindType(string typeName)
    {
        System.Type type = System.Type.GetType($"{typeName}, Assembly-CSharp");
        Assert.IsNotNull(type, $"{typeName} was not found in Assembly-CSharp.");
        return type;
    }
}
