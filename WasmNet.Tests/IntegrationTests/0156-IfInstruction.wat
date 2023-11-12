;; invoke: if (i32:0)
;; expect: (i32:100)
;; invoke: if (i32:1)
;; expect: (i32:42)

(module
    (func (export "if") (param $x i32) (result i32)
        (if (local.get $x)
            (then 
                (i32.const 42)
                return
            )
        )
        
        (i32.const 100)
        return
    )
)