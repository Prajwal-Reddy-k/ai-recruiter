import { useEffect, useRef, useState } from "react";
import { searchLocations } from "../api/locations";
import type { LocationSuggestion } from "../types";

interface LocationAutocompleteProps {
  value: string;
  onChange: (text: string) => void;
  onSelect: (suggestion: LocationSuggestion) => void;
  placeholder?: string;
}

const DEBOUNCE_MS = 400;
const MIN_CHARS = 3;

export default function LocationAutocomplete({ value, onChange, onSelect, placeholder }: LocationAutocompleteProps) {
  const [suggestions, setSuggestions] = useState<LocationSuggestion[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    if (value.trim().length < MIN_CHARS) {
      setSuggestions([]);
      setIsOpen(false);
      return;
    }

    debounceRef.current = setTimeout(async () => {
      try {
        const results = await searchLocations(value.trim());
        setSuggestions(results);
        setIsOpen(results.length > 0);
      } catch {
        // Location lookup is best-effort — plain-text entry always keeps working.
        setSuggestions([]);
        setIsOpen(false);
      }
    }, DEBOUNCE_MS);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [value]);

  function handleSelect(suggestion: LocationSuggestion) {
    onSelect(suggestion);
    setIsOpen(false);
  }

  return (
    <div className="location-autocomplete">
      <input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => suggestions.length > 0 && setIsOpen(true)}
        onBlur={() => setTimeout(() => setIsOpen(false), 150)}
        placeholder={placeholder ?? "City, state, or country"}
        autoComplete="off"
      />
      {isOpen && (
        <ul className="location-suggestions">
          {suggestions.map((s) => (
            <li key={`${s.lat}-${s.lng}`} onMouseDown={() => handleSelect(s)}>
              {s.displayName}
            </li>
          ))}
        </ul>
      )}
      <p className="osm-attribution">
        Location search powered by{" "}
        <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">
          OpenStreetMap
        </a>{" "}
        contributors. You can also just type a location freely.
      </p>
    </div>
  );
}
