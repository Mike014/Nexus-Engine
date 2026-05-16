FROM eclipse-temurin:21-jdk AS build
WORKDIR /src

COPY mvnw .
COPY .mvn .mvn
COPY pom.xml .
RUN ./mvnw dependency:go-offline

COPY src src
RUN ./mvnw package -DskipTests

FROM eclipse-temurin:21-jre AS runtime
WORKDIR /app
COPY --from=build /src/target/*.jar app.jar

EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]

# Stessa logica multi-stage. Usiamo eclipse-temurin coerentemente con quello che è stato installato localmente. 
# Il layer dependency:go-offline e' separato dal layer di build: se si cambia solo codice sorgente senza toccare il pom.xml, 
# Docker riusa il layer delle dipendenze dalla cache.