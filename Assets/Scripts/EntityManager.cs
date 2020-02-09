using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
	public static EntityManager Instance { get; private set; }

	public static List<Observer> GetActiveObservers()
	{
		return Instance?.ActiveObservers;
	}

	public static bool TryRegisterAndGetID(Observer observer, ref int id)
	{
		if (Instance == null) { return false; }
		id = Instance.RegisterAndGetID(observer);
		return true;
	}

	public static bool TryUnregister(Observer observer, int id)
	{
		if (Instance == null) { return false; }
		Instance.Unregister(observer, id);
		return true;
	}

	private List<Observer> activeObservers = new List<Observer>();
	public List<Observer> ActiveObservers
	{
		get
		{
			if (observerListDirty)
			{
				RefreshActiveObservers();
			}

			return activeObservers;
		}
	}

	private List<Observer> allObservers = new List<Observer>();
	private bool observerListDirty = false;
	private Stack<int> availableIndices = new Stack<int>();

	public void Unregister(Observer observer, int id)
	{
		if (id >= 0 && id < allObservers.Count)
		{
			allObservers[id] = null;
			availableIndices.Push(id);
			observerListDirty = true;
		}
	}

	public int RegisterAndGetID(Observer observer)
	{
		int index = -1;

		if (availableIndices.Count > 0)
		{
			index = availableIndices.Pop();
			allObservers[index] = observer;
		}
		else
		{
			allObservers.Add(observer);
			index = allObservers.Count - 1;
		}

		observerListDirty = true;

		return index;
	}

	private void Awake()
	{
		Instance = this;
	}

	private void RefreshActiveObservers()
	{
		activeObservers.Clear();
		foreach (var observer in allObservers)
		{
			if (observer != null)
			{
				activeObservers.Add(observer);
			}
		}
		observerListDirty = false;
	}
}
