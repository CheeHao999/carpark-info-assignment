# TEST_CASES.md

Comprehensive test suite documentation covering manual and automated verification scenarios for the Carpark Information Web API.

---

## Test Cases Summary Matrix

| Test Case ID | Endpoint / Feature | Objective | Input / Parameters | Expected Status |
| :--- | :--- | :--- | :--- | :--- |
| **TC-01** | `POST /api/Carparks/upload` | Verify valid CSV batch ingestion | `hdb-carpark-information-20220824010400.csv` | `200 OK` |
| **TC-02** | `POST /api/Carparks/upload` | Verify atomic transaction rollback on failure | Corrupted CSV file | `500 Internal Server Error` |
| **TC-03** | `GET /api/Carparks` | Filter carparks dynamically by criteria | `VehicleHeight=2.0&NightParking=true` | `200 OK` |
| **TC-04** | `POST /api/Carparks/favorites` | Add a valid carpark to user favorites | `{"userId": "user_01", "carParkNo": "ACB"}` | `201 Created` |
| **TC-05** | `POST /api/Carparks/favorites` | Reject duplicate favorite entry for same user | `{"userId": "user_01", "carParkNo": "ACB"}` | `409 Conflict` |
| **TC-06** | `POST /api/Carparks/favorites` | Fail gracefully when carpark does not exist | `{"userId": "user_01", "carParkNo": "INVALID"}` | `404 Not Found` |

---

## Detailed Test Scenarios

### TC-01: Successful Batch CSV Ingestion

* **Objective**: Confirm that uploading a valid HDB carpark daily delta CSV file populates the SQLite database without errors.
* **Pre-conditions**: API server is running (`dotnet run`); target CSV file exists in active directory.
* **Request**:

```bash
curl -X POST "http://localhost:5068/api/Carparks/upload" \
-F "file=@hdb-carpark-information-20220824010400.csv"
```

* **Expected Response**:

```json
{
  "message": "Batch file processed successfully."
}
```

* **Database Verification**: Querying Carparks table returns loaded records.


### TC-02: Atomic Transaction Rollback Verification

* **Objective**: Validate that the batch ingestion process implements an atomic database transaction, ensuring no partial data persists upon encountering an error.
* **Pre-conditions**: API server is running; a corrupted CSV file (e.g., missing mandatory fields, invalid numeric data) is available.
* **Request**:

```bash
# 1. Create CSV with valid headers but corrupt row data (non-numeric x_coord)
cat << 'EOF' > bad_data.csv
car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement
TEST01,Test Address,INVALID_DECIMAL_VALUE,31490,BASEMENT,ELECTRONIC,WHOLE DAY,NO,YES,1,2.0,Y
EOF

# 2. Upload corrupt file
curl -i -X POST "http://localhost:5068/api/Carparks/upload" \
  -F "file=@bad_data.csv"

# 3. Clean up temporary test file
rm bad_data.csv
```

* **Expected Response (500 Internal Server Error)**:

```plaintext
System.InvalidOperationException: Batch execution failed. Database transaction rolled back.
 ---> CsvHelper.TypeConversion.TypeConverterException: The conversion cannot be performed.
```
* **Database Verification**: No partial rows from corrupted.csv persist in the database.  

### TC-03: Dynamic Query Filtering

* **Objective**: Verify that the `/api/Carparks` endpoint correctly filters carpark records based on dynamic query parameters.
* **Pre-conditions**: Database contains various carparks with differing attributes; API server is running.
* **Request**:

```bash
curl -X GET "http://localhost:5068/api/Carparks?VehicleHeight=2.0&NightParking=true&FreeParking=false"
```

* **Expected Response (200 OK)**:

```json
[
  {
    "carParkNo": "ACB",
    "address": "BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK",
    "xCoord": 30314.7936,
    "yCoord": 31490.4942,
    "carParkType": "BASEMENT CAR PARK",
    "typeOfParkingSystem": "ELECTRONIC PARKING",
    "shortTermParking": "WHOLE DAY",
    "freeParking": "NO",
    "nightParking": true,
    "gantryHeight": 2.0,
    "carParkDecks": 1,
    "carParkBasement": true
  }
]
```

### TC-04: User Favorites Management

* **Objective**: Validate the functionality of adding carparks to user favorites, including duplicate prevention.
* **Pre-conditions**: API server is running; database contains carpark records.
* **Request (Add Favorite)**:

```bash
curl -i -X POST "http://localhost:5068/api/Carparks/favorites" \
-H "Content-Type: application/json" \
-d '{"userId": "user_01", "carParkNo": "ACB"}'
```

* **Expected Response (201 Created)**:
```json
{
  "id": 1,
  "userId": "user_01",
  "carParkNo": "ACB",
  "createdAt": "2026-08-07T14:00:00Z"
}
```


### TC-05: Duplicate Favorite Prevention

* **Objective**: Validate that the favorites endpoint prevents adding the same carpark to the same user's favorites more than once.
* **Pre-conditions**: TC-04 has been executed successfully, and "ACB" is in "user_01"'s favorites.
* **Request (Add Same Favorite)**:

```bash
curl -i -X POST "http://localhost:5068/api/Carparks/favorites" \
-H "Content-Type: application/json" \
-d '{"userId": "user_01", "carParkNo": "ACB"}'
```

* **Expected Response (409 Conflict)**:
```plaintext
Carpark is already in user's favorites.
```



### TC-06: Non-Existent Carpark Handling

* **Objective**: Validate that the favorites endpoint handles requests for carparks that do not exist in the database.
* **Pre-conditions**: API server is running; "INVALID" is not a valid CarParkNo.
* **Request**:  

```bash
curl -i -X POST "http://localhost:5068/api/Carparks/favorites" \
-H "Content-Type: application/json" \
-d '{"userId": "user_01", "carParkNo": "INVALID"}'
```

* **Expected Response (404 Not Found)**:
```json
{
  "message": "Carpark INVALID not found."
}
```

