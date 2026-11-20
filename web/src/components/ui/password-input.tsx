"use client";

import { Eye, EyeOff } from "lucide-react";
import { forwardRef, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

type PasswordInputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> & {
  className?: string;
};

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ className, ...props }, forwardedRef) => {
    const t = useTranslations("auth.passwordInput");
    const inputRef = useRef<HTMLInputElement>(null);
    const [visible, setVisible] = useState(false);

    useEffect(() => {
      const form = inputRef.current?.form;
      const hidePassword = () => setVisible(false);

      form?.addEventListener("submit", hidePassword);
      return () => {
        form?.removeEventListener("submit", hidePassword);
        setVisible(false);
      };
    }, []);

    return (
      <div className="relative">
        <Input
          ref={(node: HTMLInputElement | null) => {
            inputRef.current = node;
            if (typeof forwardedRef === "function") {
              forwardedRef(node);
            } else if (forwardedRef) {
              forwardedRef.current = node;
            }
          }}
          {...props}
          type={visible ? "text" : "password"}
          className={cn("pr-11", className)}
        />
        <button
          type="button"
          aria-label={visible ? t("hide") : t("show")}
          aria-pressed={visible}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-r-lg text-muted-foreground outline-none transition hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          onClick={() => setVisible((current) => !current)}
        >
          {visible ? <EyeOff aria-hidden="true" size={18} /> : <Eye aria-hidden="true" size={18} />}
        </button>
      </div>
    );
  },
);

PasswordInput.displayName = "PasswordInput";
