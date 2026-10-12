import { Construction } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";

interface PlaceholderPageProps {
  title: string;
  description: string;
}

export function PlaceholderPage({ title, description }: PlaceholderPageProps) {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
        <p className="text-[hsl(var(--muted-foreground))]">{description}</p>
      </div>
      <Card>
        <CardContent className="flex flex-col items-center justify-center py-16">
          <Construction className="mb-4 h-16 w-16 text-[hsl(var(--muted-foreground))]" />
          <h3 className="mb-2 text-xl font-semibold">Coming Soon</h3>
          <p className="text-center text-sm text-[hsl(var(--muted-foreground))]">
            This feature is under active development and will be available in an
            upcoming release.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
