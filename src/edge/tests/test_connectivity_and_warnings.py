import pytest

from minesense_edge.cabin_warnings import (BuzzerChannel, CabinDisplayChannel, CabinWarning,
                                           WarningDispatcher, create_channels)
from minesense_edge.config import EdgeConfig
from minesense_edge.connectivity import ConnectionState, ConnectivityMonitor

CRITICAL = CabinWarning("Critical", "Posible microsueño", 0.5, 10, 15)


def test_monitor_detects_loss_and_restoration_once():
    now = [0.0]
    monitor = ConnectivityMonitor(2, 60, clock=lambda: now[0])

    assert monitor.record_failure("timeout") is True
    assert monitor.state is ConnectionState.OFFLINE
    assert monitor.record_failure("timeout") is False
    assert monitor.record_success() is True
    assert monitor.state is ConnectionState.ONLINE
    assert monitor.record_success() is False


def test_monitor_backs_off_exponentially_up_to_the_limit():
    now = [0.0]
    monitor = ConnectivityMonitor(2, 5, clock=lambda: now[0])
    monitor.record_failure("x")          # next attempt at 2
    assert not monitor.should_attempt()
    now[0] = 2
    assert monitor.should_attempt()
    monitor.record_failure("x")          # backoff 4 -> next at 6
    now[0] = 5.9
    assert not monitor.should_attempt()
    now[0] = 6
    monitor.record_failure("x")          # backoff capped at 5 -> next at 11
    now[0] = 10.9
    assert not monitor.should_attempt()
    now[0] = 11
    assert monitor.should_attempt()


def test_warning_reaches_every_available_channel():
    result = WarningDispatcher([CabinDisplayChannel(), BuzzerChannel()]).dispatch(CRITICAL)
    assert result.delivered == ["display", "buzzer"]
    assert result.unavailable == [] and not result.fallback_used


def test_unavailable_buzzer_is_reported_and_display_still_warns(caplog):
    dispatcher = WarningDispatcher(create_channels(["buzzer"]))
    with caplog.at_level("WARNING"):
        result = dispatcher.dispatch(CRITICAL)
    assert result.delivered == ["display"]
    assert result.unavailable == ["buzzer"]
    assert dispatcher.unavailable_channels == ["buzzer"]
    assert "Medio de advertencia no disponible: buzzer" in caplog.text


def test_no_channel_left_uses_the_log_as_last_resort(caplog):
    dispatcher = WarningDispatcher(create_channels(["buzzer", "display"]))
    with caplog.at_level("CRITICAL"):
        result = dispatcher.dispatch(CRITICAL)
    assert result.fallback_used is True
    assert "NO WARNING CHANNEL AVAILABLE" in caplog.text


def test_channel_that_comes_back_is_marked_available():
    buzzer = BuzzerChannel(available=False)
    dispatcher = WarningDispatcher([CabinDisplayChannel(), buzzer])
    dispatcher.dispatch(CRITICAL)
    buzzer.available = True
    dispatcher.dispatch(CRITICAL)
    assert dispatcher.unavailable_channels == []


def test_config_from_env_defaults_and_overrides():
    config = EdgeConfig.from_env({
        "EDGE_SYNC_SERVICE_URL": "http://edge-synchronization-service:8080",
        "EDGE_PROFILE": "drowsy",
        "EDGE_BATCH_SIZE": "50",
        "EDGE_UNAVAILABLE_WARNING_CHANNELS": "Buzzer, display",
    })
    assert config.sync_service_url == "http://edge-synchronization-service:8080"
    assert config.profile == "drowsy"
    assert config.batch_size == 50
    assert config.unavailable_warning_channels == ("buzzer", "display")
    assert config.buffer_path == "data/edge-buffer.db"
    assert config.access_token is None and config.identity_url is None


@pytest.mark.parametrize("size", ["0", "501"])
def test_batch_size_must_fit_the_service_limit(size):
    with pytest.raises(ValueError):
        EdgeConfig.from_env({"EDGE_BATCH_SIZE": size})
