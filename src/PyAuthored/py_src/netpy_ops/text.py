class TextOps:
    def __init__(self, env=None):
        self.env = env
    
    def Slugify(self, input: str) -> str:
        return input.lower().strip().replace(" ", "-")
    
    async def SummarizeAsync(self, text: str) -> str:
        return (text[:120] + "...") if len(text) > 120 else text
