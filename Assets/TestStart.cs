using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Тест: кнопка UI вызывает <see cref="StartGame"/> → сервер через <see cref="CmdStartGame"/> →
/// <see cref="RpcShowStartPanel"/> на всех клиентах.
/// <para>
/// Почему раньше не работало: <c>[Command]</c> по умолчанию (<c>requiresAuthority = true</c>) может вызывать
/// только клиент, у которого этот <see cref="NetworkBehaviour"/> принадлежит локальному игроку
/// или имеет выданный authority. Скрипт на сценовом объекте без authority → предупреждение
/// «called without authority», команда не уходит, <see cref="ClientRpc"/> не вызывается.
/// </para>
/// <para>
/// Для продакшена лучше не использовать <c>requiresAuthority = false</c>, а повесить команду на
/// префаб игрока (owned) или проверять на сервере, кто нажал (connection).
/// </para>
/// </summary>
public class TestStart : NetworkBehaviour
{
    public GameObject startPanel;
    public float time = 3;

    /// <summary>На каждом клиенте после <see cref="RpcShowStartPanel"/>, когда панель включена.</summary>
    public static event Action OnStartPanelShownClients;

    /// <summary>На каждом клиенте после авто-скрытия по таймеру.</summary>
    public static event Action OnStartPanelHiddenClients;

    /// <summary>Вызывать с кнопки UI (клиент должен быть подключён и ready).</summary>
    public void StartGame()
    {
        if (!NetworkClient.active)
        {
            Debug.LogWarning("[TestStart] StartGame: нет активного клиента (кнопка на dedicated-сервере?).", this);
            return;
        }

        if (!NetworkClient.ready)
        {
            Debug.LogWarning("[TestStart] StartGame: клиент ещё не ready.", this);
            return;
        }

        // Только клиент шлёт Command на сервер; на чистом сервере UI кнопки обычно нет.
        CmdStartGame();
    }

    /// <summary>
    /// Без authority: разрешено с сценового объекта. Для реальной игры — валидация и/или Command на игроке.
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdStartGame()
    {
        RpcShowStartPanel();
    }

    [ClientRpc]
    private void RpcShowStartPanel()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            Invoke(nameof(Off), time);
        }

        OnStartPanelShownClients?.Invoke();
    }

    void Off()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        OnStartPanelHiddenClients?.Invoke();
    }
}
