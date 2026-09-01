import { useId, useMemo, useState, type KeyboardEvent } from "react";
import { Search } from "lucide-react";

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  onSubmit?: (value: string) => void;
  placeholder?: string;
  suggestions?: string[];
  "aria-label"?: string;
}

/** Free-text input with a small keyboard-navigable suggestions dropdown (prefix match against
 * the given list) — used by JobsPage's title/skill search and the Landing page hero search. */
export default function SearchBar({ value, onChange, onSubmit, placeholder, suggestions = [], ...rest }: SearchBarProps) {
  const [focused, setFocused] = useState(false);
  const [highlighted, setHighlighted] = useState(-1);
  const listId = useId();

  const matches = useMemo(() => {
    if (!value.trim()) return suggestions.slice(0, 6);
    const query = value.trim().toLowerCase();
    return suggestions.filter((s) => s.toLowerCase().includes(query) && s.toLowerCase() !== query).slice(0, 6);
  }, [value, suggestions]);

  const showDropdown = focused && matches.length > 0;

  function choose(suggestion: string) {
    onChange(suggestion);
    onSubmit?.(suggestion);
    setFocused(false);
    setHighlighted(-1);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (!showDropdown) {
      if (e.key === "Enter") onSubmit?.(value);
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlighted((i) => Math.min(i + 1, matches.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlighted((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      if (highlighted >= 0 && matches[highlighted]) {
        choose(matches[highlighted]);
      } else {
        onSubmit?.(value);
        setFocused(false);
      }
    } else if (e.key === "Escape") {
      setFocused(false);
      setHighlighted(-1);
    }
  }

  return (
    <div className="search-bar" role="combobox" aria-expanded={showDropdown} aria-haspopup="listbox" aria-owns={listId}>
      <Search size={16} className="search-bar-icon" aria-hidden="true" />
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => {
          onChange(e.target.value);
          setHighlighted(-1);
        }}
        onFocus={() => setFocused(true)}
        onBlur={() => setTimeout(() => setFocused(false), 150)}
        onKeyDown={handleKeyDown}
        role="searchbox"
        aria-autocomplete="list"
        aria-label={rest["aria-label"] ?? placeholder}
      />
      {showDropdown && (
        <ul className="search-bar-suggestions" id={listId} role="listbox">
          {matches.map((s, i) => (
            <li key={s} role="option" aria-selected={i === highlighted}>
              <button
                type="button"
                className={i === highlighted ? "search-suggestion-active" : ""}
                onMouseDown={() => choose(s)}
              >
                {s}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
