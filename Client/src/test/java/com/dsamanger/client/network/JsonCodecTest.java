package com.dsamanger.client.network;

import com.dsamanger.client.model.UserRole;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class JsonCodecTest {
    record RolePayload(UserRole role) {}

    @Test void serializesEnumsAsNamesExpectedByApi() {
        assertEquals("{\"role\":\"Admin\"}", new JsonCodec().write(new RolePayload(UserRole.Admin)));
    }
}
