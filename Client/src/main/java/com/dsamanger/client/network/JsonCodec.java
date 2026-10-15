package com.dsamanger.client.network;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.json.JsonMapper;

public final class JsonCodec {
    private final ObjectMapper mapper = JsonMapper.builder()
            .findAndAddModules()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
            .configure(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS, false)
            .build();

    public String write(Object value) {
        try { return mapper.writeValueAsString(value); }
        catch (JsonProcessingException ex) { throw new ApiException("Could not encode request JSON.", ex); }
    }

    public <T> T read(String json, Class<T> type) {
        try { return mapper.readValue(json, type); }
        catch (JsonProcessingException ex) { throw new ApiException("API response did not match the expected JSON format.", ex); }
    }
}
