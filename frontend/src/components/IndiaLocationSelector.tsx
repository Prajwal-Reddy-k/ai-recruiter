import { useEffect, useState } from "react";
import { getIndiaLocationCatalog } from "../api/locations";
import type { IndiaLocationCatalog } from "../types";
import FormField from "./ui/FormField";

interface IndiaLocationSelectorProps {
  state: string;
  city: string;
  locality: string;
  isRemote: boolean;
  onStateChange: (state: string) => void;
  onCityChange: (city: string) => void;
  onLocalityChange: (locality: string) => void;
  onIsRemoteChange: (isRemote: boolean) => void;
  stateError?: string;
  cityError?: string;
  required?: boolean;
  showRemoteOption?: boolean;
}

export default function IndiaLocationSelector({
  state,
  city,
  locality,
  isRemote,
  onStateChange,
  onCityChange,
  onLocalityChange,
  onIsRemoteChange,
  stateError,
  cityError,
  required,
  showRemoteOption = true,
}: IndiaLocationSelectorProps) {
  const [catalog, setCatalog] = useState<IndiaLocationCatalog | null>(null);
  const [cityQuery, setCityQuery] = useState("");

  useEffect(() => {
    getIndiaLocationCatalog()
      .then(setCatalog)
      .catch(() => setCatalog({ states: [] }));
  }, []);

  const selectedState = catalog?.states.find((s) => s.name === state);
  const filteredCities = (selectedState?.cities ?? []).filter((c) =>
    c.toLowerCase().includes(cityQuery.toLowerCase())
  );

  return (
    <div className="india-location-selector">
      {showRemoteOption && (
        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={isRemote}
            onChange={(e) => onIsRemoteChange(e.target.checked)}
          />
          Remote — India
        </label>
      )}

      {!isRemote && (
        <>
          <FormField label="State" htmlFor="india-state" required={required} error={stateError}>
            <select
              id="india-state"
              value={state}
              onChange={(e) => {
                onStateChange(e.target.value);
                onCityChange("");
                setCityQuery("");
              }}
            >
              <option value="">Select a state or UT</option>
              {catalog?.states.map((s) => (
                <option key={s.name} value={s.name}>
                  {s.name}
                </option>
              ))}
            </select>
          </FormField>

          <FormField label="City" htmlFor="india-city" required={required} error={cityError}>
            <input
              id="india-city"
              list="india-city-options"
              value={city}
              disabled={!state}
              onChange={(e) => {
                onCityChange(e.target.value);
                setCityQuery(e.target.value);
              }}
              placeholder={state ? "Search cities in this state" : "Select a state first"}
            />
            <datalist id="india-city-options">
              {filteredCities.map((c) => (
                <option key={c} value={c} />
              ))}
            </datalist>
          </FormField>

          <FormField label="Locality (optional)" htmlFor="india-locality">
            <input
              id="india-locality"
              value={locality}
              onChange={(e) => onLocalityChange(e.target.value)}
              placeholder="e.g. Whitefield, Koramangala"
            />
          </FormField>
        </>
      )}
    </div>
  );
}
